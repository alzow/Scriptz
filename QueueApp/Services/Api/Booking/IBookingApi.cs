using Refit;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Api.Booking;

public interface IBookingApi
{
    [Post("/rpc/get_available_slots")]
    Task<List<SlotResponse>> GetAvailableSlotsAsync([Body] GetAvailableSlotsRequest request);

    // Pooled path — no operator chosen, slots free on any resource at the business.
    [Post("/rpc/get_available_slots_any")]
    Task<List<SlotResponse>> GetAvailableSlotsAnyAsync([Body] GetAvailableSlotsAnyRequest request);

    [Post("/rpc/create_booking")]
    Task<BookingResponse> CreateBookingAsync([Body] CreateBookingRequest request);

    // Pooled path — server assigns whichever resource is actually free at that slot.
    [Post("/rpc/create_booking_any")]
    Task<BookingResponse> CreateBookingAnyAsync([Body] CreateBookingAnyRequest request);

    [Post("/rpc/cancel_booking")]
    Task<BookingResponse> CancelBookingAsync([Body] CancelBookingRequest request);

    [Post("/rpc/confirm_booking")]
    Task<BookingResponse> ConfirmBookingAsync([Body] CancelBookingRequest request);

    [Post("/rpc/complete_booking")]
    Task<BookingResponse> CompleteBookingAsync([Body] CancelBookingRequest request);

    // Optional, quiet staff-facing note on a booking — same idea as set_queue_progress.
    [Post("/rpc/set_booking_progress")]
    Task<BookingResponse> SetBookingProgressAsync([Body] SetBookingProgressRequest request);

    [Get("/bookings?select=id,starts_at,ends_at,status,operator:operators(display_name),service:services(name),progress_status&order=created_at.desc&limit=5")]
    Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(
        [AliasAs("business_id")] string businessId,
        [AliasAs("customer_id")] string customerId);

    [Get("/bookings?select=id,starts_at,ends_at,status,operator:operators(display_name),service:services(name),customer:profiles(display_name),progress_status&order=starts_at.asc")]
    Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(
        [AliasAs("business_id")] string businessId,
        [AliasAs("and")] string dateRangeFilter);

    [Get("/bookings?select=id,starts_at,ends_at,status,business:businesses(id,name,category,allow_operator_choice),operator:operators(display_name),service:services(name),progress_status&status=in.(pending,confirmed)&order=starts_at.asc")]
    Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync([AliasAs("customer_id")] string customerId);

    [Get("/bookings?select=id,starts_at,ends_at,status,business:businesses(id,name,category,allow_operator_choice),operator:operators(display_name),service:services(name)&order=starts_at.desc")]
    Task<List<UpcomingBookingResponse>> GetMyBookingHistoryAsync([AliasAs("customer_id")] string customerId);
}
