using Refit;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Api.Booking;

public interface IBookingApi
{
    [Post("/rpc/get_available_slots")]
    Task<List<SlotResponse>> GetAvailableSlotsAsync([Body] GetAvailableSlotsRequest request);

    [Post("/rpc/create_booking")]
    Task<BookingResponse> CreateBookingAsync([Body] CreateBookingRequest request);

    [Post("/rpc/cancel_booking")]
    Task<BookingResponse> CancelBookingAsync([Body] CancelBookingRequest request);

    [Post("/rpc/confirm_booking")]
    Task<BookingResponse> ConfirmBookingAsync([Body] CancelBookingRequest request);

    [Post("/rpc/complete_booking")]
    Task<BookingResponse> CompleteBookingAsync([Body] CancelBookingRequest request);

    [Get("/bookings?select=id,starts_at,ends_at,status,operator:operators(display_name),service:services(name)&order=created_at.desc&limit=5")]
    Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(
        [AliasAs("business_id")] string businessId,
        [AliasAs("customer_id")] string customerId);

    [Get("/bookings?select=id,starts_at,ends_at,status,operator:operators(display_name),service:services(name),customer:profiles(display_name)&order=starts_at.asc")]
    Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(
        [AliasAs("business_id")] string businessId,
        [AliasAs("and")] string dateRangeFilter);

    [Get("/bookings?select=id,starts_at,ends_at,status,business:businesses(name),operator:operators(display_name),service:services(name)&status=in.(pending,confirmed)&order=starts_at.asc")]
    Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync([AliasAs("customer_id")] string customerId);
}
