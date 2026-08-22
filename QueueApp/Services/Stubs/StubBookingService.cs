using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Booking screen can be fully tested without a Supabase project.
// Registered instead of the real BookingService in DEBUG builds.
public class StubBookingService : IBookingService
{
    private readonly List<BookingResponse> _bookings = new();

    public Task<List<SlotResponse>> GetAvailableSlotsAsync(Guid operatorId, Guid serviceId, DateTime date)
    {
        var day = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var slots = Enumerable.Range(0, 16)
            .Select(i => day.AddHours(9).AddMinutes(i * 30))
            .Where(start => !_bookings.Any(b =>
                b.OperatorId == operatorId && b.Status != "cancelled" &&
                b.StartsAt.UtcDateTime == start))
            .Select(start => new SlotResponse
            {
                SlotStart = new DateTimeOffset(start, TimeSpan.Zero),
                SlotEnd = new DateTimeOffset(start.AddMinutes(30), TimeSpan.Zero),
            })
            .ToList();
        return Task.FromResult(slots);
    }

    public Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request)
    {
        var booking = new BookingResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            OperatorId = request.OperatorId,
            ServiceId = request.ServiceId,
            CustomerId = request.CustomerId,
            StartsAt = request.StartsAt,
            EndsAt = request.StartsAt.AddMinutes(30),
            Status = "pending",
            Note = request.Note,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _bookings.Add(booking);
        return Task.FromResult(booking);
    }

    public Task<BookingResponse> CancelBookingAsync(Guid bookingId)
    {
        var booking = _bookings.First(b => b.Id == bookingId);
        booking.Status = "cancelled";
        return Task.FromResult(booking);
    }

    public Task<BookingResponse> ConfirmBookingAsync(Guid bookingId)
    {
        var booking = _bookings.First(b => b.Id == bookingId);
        booking.Status = "confirmed";
        return Task.FromResult(booking);
    }

    public Task<BookingResponse> CompleteBookingAsync(Guid bookingId)
    {
        var booking = _bookings.First(b => b.Id == bookingId);
        booking.Status = "completed";
        return Task.FromResult(booking);
    }

    public Task<List<MyBookingSummaryResponse>> GetMyBookingsAsync(Guid businessId, Guid customerId)
    {
        var summaries = _bookings
            .Where(b => b.BusinessId == businessId && b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new MyBookingSummaryResponse
            {
                Id = b.Id,
                StartsAt = b.StartsAt,
                EndsAt = b.EndsAt,
                Status = b.Status,
            })
            .ToList();
        return Task.FromResult(summaries);
    }

    public Task<List<AgendaBookingResponse>> GetAgendaBookingsAsync(Guid businessId, DateTime date)
    {
        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        var agenda = _bookings
            .Where(b => b.BusinessId == businessId &&
                        b.StartsAt.UtcDateTime >= dayStart && b.StartsAt.UtcDateTime < dayEnd)
            .OrderBy(b => b.StartsAt)
            .Select(b => new AgendaBookingResponse
            {
                Id = b.Id,
                StartsAt = b.StartsAt,
                EndsAt = b.EndsAt,
                Status = b.Status,
            })
            .ToList();
        return Task.FromResult(agenda);
    }
}
