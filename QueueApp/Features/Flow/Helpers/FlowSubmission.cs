using QueueApp.Features.Flow.Constants;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Auth;

namespace QueueApp.Features.Flow.Helpers;

public sealed record FlowSubmissionRequest
{
    public required Guid BusinessId { get; init; }
    public required Guid ServiceId { get; init; }
    public Guid? OperatorId { get; init; }
    public bool IsAnyAvailable { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
    public string? Note { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public Dictionary<string, IntakeAnswer>? IntakeResponses { get; init; }
}

public sealed record FlowSubmissionResult(Guid RecordId, bool IsBooking);

public sealed class FlowSubmissionCoordinator
{
    private readonly IBookingService _bookingService;
    private readonly IQueueService _queueService;
    private readonly IAuthService _authService;
    private readonly IProfileService _profileService;

    public FlowSubmissionCoordinator(
        IBookingService bookingService,
        IQueueService queueService,
        IAuthService authService,
        IProfileService profileService)
    {
        _bookingService = bookingService;
        _queueService = queueService;
        _authService = authService;
        _profileService = profileService;
    }

    // The shop's own booking is a direct insert rather than create_booking: there is no customer_id
    // to supply and nobody left to confirm with, so it goes in already confirmed with whatever name
    // and number the operator was given.
    public async Task<FlowSubmissionResult> SubmitOperatorBookingAsync(FlowSubmissionRequest request)
    {
        if (request.OperatorId is not { } operatorId)
            throw new InvalidOperationException(FlowConstants.NoOperatorForBookingError);

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new InvalidOperationException(FlowConstants.NoCustomerNameError);

        var booking = await _bookingService.CreateOperatorBookingAsync(new CreateOperatorBookingRequest
        {
            BusinessId = request.BusinessId,
            OperatorId = operatorId,
            ServiceId = request.ServiceId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = BookingStatuses.Confirmed,
            Note = request.Note,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? null : request.CustomerPhone.Trim(),
            Details = new BookingDetails { CreatedBy = FlowConstants.CreatedByOperator },
            IntakeResponses = request.IntakeResponses,
        });

        return new FlowSubmissionResult(booking?.Id ?? Guid.Empty, IsBooking: true);
    }

    public async Task<FlowSubmissionResult> SubmitBookingAsync(FlowSubmissionRequest request)
    {
        var customerId = await RequireUserIdAsync();

        var booking = request.IsAnyAvailable
            ? await _bookingService.CreateBookingAnyAsync(new CreateBookingAnyRequest
            {
                BusinessId = request.BusinessId,
                ServiceId = request.ServiceId,
                CustomerId = customerId,
                StartsAt = request.StartsAt,
                Note = request.Note,
                IntakeResponses = request.IntakeResponses,
            })
            : await _bookingService.CreateBookingAsync(new CreateBookingRequest
            {
                BusinessId = request.BusinessId,
                OperatorId = request.OperatorId!.Value,
                ServiceId = request.ServiceId,
                CustomerId = customerId,
                StartsAt = request.StartsAt,
                Note = request.Note,
                IntakeResponses = request.IntakeResponses,
            });

        await StampBookingCustomerNameAsync(booking.Id, customerId);

        return new FlowSubmissionResult(booking.Id, IsBooking: true);
    }

    public async Task<FlowSubmissionResult> SubmitJoinAsync(FlowSubmissionRequest request)
    {
        var customerId = await RequireUserIdAsync();
        var customerName = await _profileService.GetMyDisplayNameAsync(customerId);

        var entry = await _queueService.JoinQueueAsync(
            request.BusinessId,
            request.OperatorId,
            customerId,
            customerName,
            request.ServiceId,
            request.IntakeResponses);

        return new FlowSubmissionResult(entry.Id, IsBooking: false);
    }

    private async Task<Guid> RequireUserIdAsync()
    {
        var userId = await _authService.GetUserIdAsync();

        return string.IsNullOrEmpty(userId)
            ? throw new InvalidOperationException(FlowConstants.NoSignedInUserError)
            : Guid.Parse(userId);
    }

    // create_booking has no name parameter and the shop cannot read the customer's profile, so the
    // booking would land on the agenda as "Customer". The customer owns the row they just created,
    // so they write their own name onto it. Best effort: a booking with no name on it beats failing
    // a booking that already succeeded.
    private async Task StampBookingCustomerNameAsync(Guid bookingId, Guid userId)
    {
        try
        {
            var profile = await _profileService.GetMyProfileAsync(userId);
            if (profile is null || string.IsNullOrWhiteSpace(profile.DisplayName))
                return;

            await _bookingService.SetCustomerNameAsync(bookingId, profile.DisplayName);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Could not stamp the booking's customer name: {exception.Message}");
        }
    }
}
