using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Api.Booking;

public interface IBookingService
{
    Task<List<SlotResponse>> GetAvailableSlotsAsync(Guid operatorId, Guid serviceId, DateTime date);
    Task<List<SlotResponse>> GetAvailableSlotsAnyAsync(Guid businessId, Guid serviceId, DateTime date);
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request);
    Task<BookingResponse> CreateBookingAnyAsync(CreateBookingAnyRequest request);
    Task<BookingResponse> CancelBookingAsync(Guid bookingId);
    Task<BookingResponse> ConfirmBookingAsync(Guid bookingId);
    Task<BookingResponse> CompleteBookingAsync(Guid bookingId);

    // TODO: PATCH-based by design (see Documentation/awaiting-collection-backend-requirements.md
    // §4) — no state-machine enforcement. Revisit with a dedicated RPC if that becomes a problem.
    Task<AgendaBookingResponse?> MarkBookingAwaitingCollectionAsync(Guid bookingId);
    Task<AgendaBookingResponse?> MarkBookingCollectedAsync(Guid bookingId);
    Task<BookingResponse> SetBookingProgressAsync(Guid bookingId, string? status);
    Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(Guid businessId, Guid customerId);
    Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(Guid businessId, DateTime date);
    Task<List<AgendaBookingResponse>> GetPendingRequestsAsync(Guid businessId, DateTime fromDate, int days);
    Task<List<AgendaBookingResponse>> GetBookingsInRangeAsync(Guid businessId, DateTimeOffset from, DateTimeOffset until);
    Task<AgendaBookingResponse?> MarkBookingNoShowAsync(Guid bookingId);
    Task<AgendaBookingResponse?> SetCancellationReasonAsync(Guid bookingId, BookingDetails details);
    Task<AgendaBookingResponse?> SetCustomerNameAsync(Guid bookingId, string customerName);
    Task<AgendaBookingResponse?> MoveBookingAsync(Guid bookingId, Guid operatorId, DateTimeOffset startsAt, DateTimeOffset endsAt);
    Task<AgendaBookingResponse?> CreateOperatorBookingAsync(CreateOperatorBookingRequest request);
    Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync(Guid customerId);
    Task<List<UpcomingBookingResponse>> GetMyBookingHistoryAsync(Guid customerId);
    Task<UpcomingBookingResponse?> GetBookingAsync(Guid bookingId);

    // cancel_booking takes no "who", so the customer stamps their own cancellation into details
    // before the RPC runs — the shop's path already writes the reason the same way.
    Task<AgendaBookingResponse?> MarkCancelledByCustomerAsync(Guid bookingId, BookingDetails? existing);
}
