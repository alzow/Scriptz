using System.Net;
using Refit;
using QueueApp.Features.Flow.Constants;
using QueueApp.Features.Flow.Helpers;

namespace QueueApp.Features.Flow;

public abstract partial class FlowPageViewModelBase
{
    public async Task SubmitAsync()
    {
        if (SelectedServiceRow is null)
            return;

        if (IsSlotFlow && SelectedSlot is null)
            return;

        IsSubmitting = true;
        try
        {
            var request = BuildSubmissionRequest();

            var result = IsOperatorFlow
                ? await _submission.SubmitOperatorBookingAsync(request)
                : IsBookingMode
                    ? await _submission.SubmitBookingAsync(request)
                    : await _submission.SubmitJoinAsync(request);

            _submittedRecordId = result.RecordId;
            _submittedIsBooking = result.IsBooking;

            ResetFlowState();
            await OnSubmittedAsync();
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            // bookings_no_overlap caught a race — someone took this exact slot between the list
            // loading and the confirm tap.
            await HandleExceptionAsync(new InvalidOperationException(IsOperatorFlow
                ? FlowConstants.SlotTakenByShopError
                : FlowConstants.SlotTakenByCustomerError));

            _schedule.InvalidateSlots();
            await LoadSlotsAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public FlowSubmissionRequest BuildSubmissionRequest() => new()
    {
        BusinessId = _businessId,
        ServiceId = SelectedServiceRow!.Service.Id,
        OperatorId = SelectedOperatorChoice?.OperatorId,
        IsAnyAvailable = SelectedOperatorChoice?.IsAnyAvailable ?? false,
        StartsAt = SelectedSlot?.Slot.SlotStart ?? default,
        EndsAt = SelectedSlot?.Slot.SlotEnd ?? default,
        Note = TrimmedBookingNote(),
        CustomerName = CustomerName,
        CustomerPhone = CustomerPhone,
        IntakeResponses = _intake.BuildResponses(),
    };

    public string? TrimmedBookingNote() =>
        string.IsNullOrWhiteSpace(BookingNote) ? null : BookingNote.Trim();
}
