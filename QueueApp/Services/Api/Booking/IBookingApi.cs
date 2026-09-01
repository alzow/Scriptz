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

    [Get("/bookings?select=id,starts_at,ends_at,status,operator:operators(display_name),service:services(name,price_cents),progress_status&order=created_at.desc&limit=5")]
    Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(
        [AliasAs("business_id")] string businessId,
        [AliasAs("customer_id")] string customerId);

    // `*` rather than a column list on purpose: started_at doesn't exist on the bookings table yet
    // (see Documentation/STEP-18-BOOKING-AGENDA-SUPABASE.md), and naming a column PostgREST can't
    // find fails the whole query with a 400. With `*` the agenda keeps working today and picks the
    // column up by itself the moment the migration lands.
    [Get("/bookings?select=*,operator:operators(id,display_name),service:services(id,name,price_cents,est_minutes),customer:profiles(display_name,phone)&order=starts_at.asc")]
    Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(
        [AliasAs("business_id")] string businessId,
        [AliasAs("and")] string dateRangeFilter);

    // Every request still waiting on the operator across the whole day strip, not just the day
    // being looked at — otherwise Thursday's request stays invisible until Thursday.
    [Get("/bookings?select=*,operator:operators(id,display_name),service:services(id,name,price_cents,est_minutes),customer:profiles(display_name,phone)&status=eq.pending&order=created_at.asc")]
    Task<List<AgendaBookingResponse>> GetPendingRequestsAsync(
        [AliasAs("business_id")] string businessId,
        [AliasAs("and")] string dateRangeFilter);

    // Start / no-show / move all go through the table's owner-update RLS policy rather than an RPC —
    // "owner or self manage" already permits them, so none of the three needs new SQL.
    [Patch("/bookings")]
    Task<List<AgendaBookingResponse>> UpdateBookingAsync(
        [AliasAs("id")] string idEq,
        [Body] UpdateBookingRequest request);

    // Operator-created booking: a direct insert, permitted by `is_business_owner(business_id)` on
    // the bookings insert policy. create_booking is the customer path and needs a real customer_id;
    // a phone booking has nobody to attach.
    [Post("/bookings")]
    Task<List<AgendaBookingResponse>> CreateBookingRowAsync([Body] CreateOperatorBookingRequest request);

    [Get("/bookings?select=id,starts_at,ends_at,status,created_at,business:businesses(id,name,category,allow_operator_choice),operator:operators(display_name),service:services(name,price_cents),progress_status,note,details&status=in.(pending,confirmed)&order=starts_at.asc")]
    Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync([AliasAs("customer_id")] string customerId);

    [Get("/bookings?select=id,starts_at,ends_at,status,created_at,business:businesses(id,name,category,allow_operator_choice),operator:operators(display_name),service:services(name,price_cents),note,details&order=starts_at.desc")]
    Task<List<UpcomingBookingResponse>> GetMyBookingHistoryAsync([AliasAs("customer_id")] string customerId);

    // One booking, in the same projection the history list uses — VisitPage loads from an id
    // because the row that was tapped may be stale.
    [Get("/bookings?select=id,starts_at,ends_at,status,created_at,business:businesses(id,name,category,allow_operator_choice),operator:operators(display_name),service:services(name,price_cents),note,details")]
    Task<List<UpcomingBookingResponse>> GetBookingAsync([AliasAs("id")] string idEq);
}
