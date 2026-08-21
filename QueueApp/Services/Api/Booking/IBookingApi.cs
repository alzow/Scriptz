using Refit;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Api.Booking;

public interface IBookingApi
{
    [Post("/rpc/get_available_slots")]
    Task<List<SlotResponse>> GetAvailableSlotsAsync([Body] GetAvailableSlotsRequest request);

    [Post("/rpc/create_booking")]
    Task<BookingResponse> CreateBookingAsync([Body] CreateBookingRequest request);
}
