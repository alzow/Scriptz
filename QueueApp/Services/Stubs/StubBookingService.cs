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

    // Pooled path — no operator distinction to check conflicts against in this stub, so any date
    // that isn't already fully booked business-wide reads as available. Good enough to demo the
    // "no bay picker" flow; real conflict/resource logic lives in get_available_slots_any.
    public Task<List<SlotResponse>> GetAvailableSlotsAnyAsync(Guid businessId, Guid serviceId, DateTime date)
    {
        var day = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var slots = Enumerable.Range(0, 16)
            .Select(i => day.AddHours(9).AddMinutes(i * 30))
            .Where(start => !_bookings.Any(b =>
                b.BusinessId == businessId && b.Status != "cancelled" &&
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

    // No operator to assign in this stub (no resource pool to pick from) — server-side
    // create_booking_any would resolve a real operator_id; here the booking just carries none.
    public Task<BookingResponse> CreateBookingAnyAsync(CreateBookingAnyRequest request)
    {
        var booking = new BookingResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            OperatorId = Guid.Empty,
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

    public Task<BookingResponse> SetBookingProgressAsync(Guid bookingId, string? status)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking != null) booking.ProgressStatus = status;
        return Task.FromResult(booking ?? new BookingResponse { Id = bookingId, ProgressStatus = status });
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
                ProgressStatus = b.ProgressStatus,
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
                ProgressStatus = b.ProgressStatus,
            })
            .ToList();
        return Task.FromResult(agenda);
    }

    public Task<List<UpcomingBookingResponse>> GetMyUpcomingBookingsAsync(Guid customerId)
    {
        var upcoming = _bookings
            .Where(b => b.CustomerId == customerId && b.Status is "pending" or "confirmed")
            .OrderBy(b => b.StartsAt)
            .Select(b => new UpcomingBookingResponse
            {
                Id = b.Id,
                StartsAt = b.StartsAt,
                EndsAt = b.EndsAt,
                Status = b.Status,
                ProgressStatus = b.ProgressStatus,
            })
            .ToList();
        return Task.FromResult(upcoming);
    }

    public Task<List<UpcomingBookingResponse>> GetMyBookingHistoryAsync(Guid customerId)
    {
        var history = _bookings
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.StartsAt)
            .Select(b => new UpcomingBookingResponse
            {
                Id = b.Id,
                StartsAt = b.StartsAt,
                EndsAt = b.EndsAt,
                Status = b.Status,
            })
            .ToList();
        return Task.FromResult(history);
    }
}
