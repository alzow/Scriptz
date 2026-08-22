using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Api.Booking;

public interface IBookingService
{
    Task<List<SlotResponse>> GetAvailableSlotsAsync(Guid operatorId, Guid serviceId, DateTime date);
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request);
    Task<BookingResponse> CancelBookingAsync(Guid bookingId);
    Task<BookingResponse> ConfirmBookingAsync(Guid bookingId);
    Task<BookingResponse> CompleteBookingAsync(Guid bookingId);
    Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(Guid businessId, Guid customerId);
    Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(Guid businessId, DateTime date);
}
