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
    Task<BookingResponse> SetBookingProgressAsync(Guid bookingId, string? status);
    Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(Guid businessId, Guid customerId);
    Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(Guid businessId, DateTime date);
    Task<List<AgendaBookingResponse>> GetPendingRequestsAsync(Guid businessId, DateTime fromDate, int days);
    Task<List<AgendaBookingResponse>> GetBookingsInRangeAsync(Guid businessId, DateTimeOffset from, DateTimeOffset until);
    Task<AgendaBookingResponse?> MarkBookingNoShowAsync(Guid bookingId);
    Task<AgendaBookingResponse?> SetCancellationReasonAsync(Guid bookingId, BookingDetails details);
    Task<AgendaBookingResponse?> MoveBookingAsync(Guid bookingId, Guid operatorId, DateTimeOffset startsAt, DateTimeOffset endsAt);
    Task<AgendaBookingResponse?> CreateOperatorBookingAsync(CreateOperatorBookingRequest request);
    Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync(Guid customerId);
    Task<List<UpcomingBookingResponse>> GetMyBookingHistoryAsync(Guid customerId);
}
