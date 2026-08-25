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

    public Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request) =>
        ExecuteApiCallAsync(_api.CreateBookingAsync(request));

    public Task<BookingResponse> CancelBookingAsync(Guid bookingId) =>
        ExecuteApiCallAsync(_api.CancelBookingAsync(new CancelBookingRequest { BookingId = bookingId }));

    public Task<BookingResponse> ConfirmBookingAsync(Guid bookingId) =>
        ExecuteApiCallAsync(_api.ConfirmBookingAsync(new CancelBookingRequest { BookingId = bookingId }));

    public Task<BookingResponse> CompleteBookingAsync(Guid bookingId) =>
        ExecuteApiCallAsync(_api.CompleteBookingAsync(new CancelBookingRequest { BookingId = bookingId }));

    public Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(Guid businessId, Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyBookingsAsync($"eq.{businessId}", $"eq.{customerId}"));

    public Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(Guid businessId, DateTime date)
    {
        var dayStart = new DateTimeOffset(date.Date, TimeSpan.FromHours(2));
        var dayEnd = dayStart.AddDays(1);

        var filter = $"(starts_at.gte.{dayStart:yyyy-MM-ddTHH:mm:sszzz},starts_at.lt.{dayEnd:yyyy-MM-ddTHH:mm:sszzz})";

        return ExecuteApiCallAsync(_api.GetAgendaBookingsAsync($"eq.{businessId}", filter));
    }

    public Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync(Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyUpcomingBookingsAsync($"eq.{customerId}"));

    public Task<List<UpcomingBookingResponse>> GetMyBookingHistoryAsync(Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyBookingHistoryAsync($"eq.{customerId}"));
}
