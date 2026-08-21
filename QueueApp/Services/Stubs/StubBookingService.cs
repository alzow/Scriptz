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
}
