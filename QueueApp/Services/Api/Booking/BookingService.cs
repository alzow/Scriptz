using QueueApp.Framework.Base;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Api.Booking;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class BookingService : BaseService, IBookingService
{
    private readonly IBookingApi _api;

    public BookingService(IBookingApi api)
    {
        _api = api;
    }

    public Task<List<SlotResponse>> GetAvailableSlotsAsync(Guid operatorId, Guid serviceId, DateTime date) =>
        ExecuteApiCallAsync(_api.GetAvailableSlotsAsync(new GetAvailableSlotsRequest
        {
            OperatorId = operatorId,
            ServiceId = serviceId,
            Date = date.ToString("yyyy-MM-dd"),
        }));

    public Task<List<SlotResponse>> GetAvailableSlotsAnyAsync(Guid businessId, Guid serviceId, DateTime date) =>
        ExecuteApiCallAsync(_api.GetAvailableSlotsAnyAsync(new GetAvailableSlotsAnyRequest
        {
            BusinessId = businessId,
            ServiceId = serviceId,
            Date = date.ToString("yyyy-MM-dd"),
        }));

    public Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request) =>
        ExecuteApiCallAsync(_api.CreateBookingAsync(request));

    public Task<BookingResponse> CreateBookingAnyAsync(CreateBookingAnyRequest request) =>
        ExecuteApiCallAsync(_api.CreateBookingAnyAsync(request));

    public Task<BookingResponse> SetBookingProgressAsync(Guid bookingId, string? status) =>
        ExecuteApiCallAsync(_api.SetBookingProgressAsync(new SetBookingProgressRequest { BookingId = bookingId, Status = status }));

    public Task<BookingResponse> CancelBookingAsync(Guid bookingId) =>
        ExecuteApiCallAsync(_api.CancelBookingAsync(new CancelBookingRequest { BookingId = bookingId }));

    public Task<BookingResponse> ConfirmBookingAsync(Guid bookingId) =>
        ExecuteApiCallAsync(_api.ConfirmBookingAsync(new CancelBookingRequest { BookingId = bookingId }));

    public Task<BookingResponse> CompleteBookingAsync(Guid bookingId) =>
        ExecuteApiCallAsync(_api.CompleteBookingAsync(new CancelBookingRequest { BookingId = bookingId }));

    public Task<AgendaBookingResponse?> MarkBookingAwaitingCollectionAsync(Guid bookingId) =>
        PatchAsync(bookingId, new UpdateBookingRequest { Status = BookingStatuses.AwaitingCollection });

    public Task<AgendaBookingResponse?> MarkBookingCollectedAsync(Guid bookingId) =>
        PatchAsync(bookingId, new UpdateBookingRequest { Status = BookingStatuses.Completed });

    public Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(Guid businessId, Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyBookingsAsync($"eq.{businessId}", $"eq.{customerId}"));

    public Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(Guid businessId, DateTime date)
    {
        var dayStart = new DateTimeOffset(date.Date, SastOffset);
        return ExecuteApiCallAsync(
            _api.GetAgendaBookingsAsync($"eq.{businessId}", StartsWithinFilter(dayStart, dayStart.AddDays(1))));
    }

    public Task<List<AgendaBookingResponse>> GetPendingRequestsAsync(Guid businessId, DateTime fromDate, int days)
    {
        var from = new DateTimeOffset(fromDate.Date, SastOffset);
        return ExecuteApiCallAsync(
            _api.GetPendingRequestsAsync($"eq.{businessId}", StartsWithinFilter(from, from.AddDays(days))));
    }

    // Same projection as the agenda, over an arbitrary span — what "blocking this range will strand
    // these customers" needs when the range runs past the day being looked at. Deliberately
    // unfiltered by status: `no_show` and `in_progress` may not exist in the booking_status enum
    // yet, and PostgREST rejects the whole query for an enum label it can't parse.
    public Task<List<AgendaBookingResponse>> GetBookingsInRangeAsync(
        Guid businessId, DateTimeOffset from, DateTimeOffset until) =>
        ExecuteApiCallAsync(_api.GetAgendaBookingsAsync($"eq.{businessId}", StartsWithinFilter(from, until)));

    public Task<AgendaBookingResponse?> MarkBookingNoShowAsync(Guid bookingId) =>
        PatchAsync(bookingId, new UpdateBookingRequest { Status = BookingStatuses.NoShow });

    // Written before cancel_booking runs, because that RPC takes no reason and the details jsonb is
    // the only place to put one without a migration.
    public Task<AgendaBookingResponse?> SetCancellationReasonAsync(Guid bookingId, BookingDetails details) =>
        PatchAsync(bookingId, new UpdateBookingRequest { Details = details });

    // create_booking takes no name, and the agenda's customer:profiles embed comes back null for the
    // shop (profiles is self-read only), so a customer-made booking reads as "Customer" until the
    // Step 18 §3 migration lands. The customer owns the row they just created, so they write their
    // own name into the details jsonb — the one place the owner can read it from today. Same trick
    // as the cancellation reason, and no migration needed.
    //
    // Name only, deliberately: bookings is still public read on the current schema, and the phone
    // number is exactly what Step 18 §4 exists to stop publishing to every signed-in user.
    //
    // TODO: drop this once Step 18 §3's customer_name column and fill_booking_customer_snapshot
    // trigger are applied — the trigger fills the column and the reader prefers it over details.
    public Task<AgendaBookingResponse?> SetCustomerNameAsync(Guid bookingId, string customerName) =>
        PatchAsync(bookingId, new UpdateBookingRequest
        {
            Details = new BookingDetails
            {
                CustomerName = customerName.Trim(),
                CreatedBy = "customer",
            },
        });

    public Task<AgendaBookingResponse?> MoveBookingAsync(
        Guid bookingId, Guid operatorId, DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        PatchAsync(bookingId, new UpdateBookingRequest
        {
            OperatorId = operatorId,
            StartsAt = startsAt,
            EndsAt = endsAt,
        });

    public async Task<AgendaBookingResponse?> CreateOperatorBookingAsync(CreateOperatorBookingRequest request)
    {
        var rows = await ExecuteApiCallAsync(_api.CreateBookingRowAsync(request));
        return rows.FirstOrDefault();
    }

    private async Task<AgendaBookingResponse?> PatchAsync(Guid bookingId, UpdateBookingRequest request)
    {
        var rows = await ExecuteApiCallAsync(_api.UpdateBookingAsync($"eq.{bookingId}", request));
        return rows.FirstOrDefault();
    }

    // PostgREST needs both halves of a range in one `and=(…)` group — two separate starts_at query
    // parameters would collide rather than intersect.
    private static string StartsWithinFilter(DateTimeOffset from, DateTimeOffset until) =>
        $"(starts_at.gte.{from:yyyy-MM-ddTHH:mm:sszzz},starts_at.lt.{until:yyyy-MM-ddTHH:mm:sszzz})";

    private static readonly TimeSpan SastOffset = TimeSpan.FromHours(2);

    public Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync(Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyUpcomingBookingsAsync($"eq.{customerId}"));

    public Task<List<UpcomingBookingResponse>> GetMyBookingHistoryAsync(Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyBookingHistoryAsync($"eq.{customerId}"));

    public async Task<UpcomingBookingResponse?> GetBookingAsync(Guid bookingId)
    {
        var rows = await ExecuteApiCallAsync(_api.GetBookingAsync($"eq.{bookingId}"));
        return rows.FirstOrDefault();
    }

    public Task<AgendaBookingResponse?> MarkCancelledByCustomerAsync(Guid bookingId, BookingDetails? existing) =>
        PatchAsync(bookingId, new UpdateBookingRequest { Details = BookingDetails.CancelledByCustomer(existing) });
}
