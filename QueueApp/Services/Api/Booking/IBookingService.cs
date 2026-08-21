using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Api.Booking;

public interface IBookingService
{
    Task<List<SlotResponse>> GetAvailableSlotsAsync(Guid operatorId, Guid serviceId, DateTime date);
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request);
}
