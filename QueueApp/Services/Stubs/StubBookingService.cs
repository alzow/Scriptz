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

    public Task<BookingResponse> CancelBookingAsync(Guid bookingId) =>
        Task.FromResult(SetStatus(bookingId, BookingStatuses.Cancelled));

    public Task<BookingResponse> ConfirmBookingAsync(Guid bookingId) =>
        Task.FromResult(SetStatus(bookingId, BookingStatuses.Confirmed));

    public Task<BookingResponse> CompleteBookingAsync(Guid bookingId) =>
        Task.FromResult(SetStatus(bookingId, BookingStatuses.Completed));

    public Task<AgendaBookingResponse?> MarkBookingAwaitingCollectionAsync(Guid bookingId)
    {
        var booking = _agenda.FirstOrDefault(b => b.Id == bookingId);
        if (booking is not null) booking.Status = BookingStatuses.AwaitingCollection;
        else SetStatus(bookingId, BookingStatuses.AwaitingCollection);

        return Task.FromResult<AgendaBookingResponse?>(booking);
    }

    public Task<AgendaBookingResponse?> MarkBookingCollectedAsync(Guid bookingId)
    {
        var booking = _agenda.FirstOrDefault(b => b.Id == bookingId);
        if (booking is not null) booking.Status = BookingStatuses.Completed;
        else SetStatus(bookingId, BookingStatuses.Completed);

        return Task.FromResult<AgendaBookingResponse?>(booking);
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
        var from = new DateTimeOffset(date.Date, SastOffset);
        return GetBookingsInRangeAsync(businessId, from, from.AddDays(1));
    }

    public Task<List<AgendaBookingResponse>> GetBookingsInRangeAsync(
        Guid businessId, DateTimeOffset from, DateTimeOffset until) =>
        Task.FromResult(InRange(businessId, from, until));

    public Task<List<AgendaBookingResponse>> GetPendingRequestsAsync(Guid businessId, DateTime fromDate, int days)
    {
        var from = new DateTimeOffset(fromDate.Date, SastOffset);
        var pending = InRange(businessId, from, from.AddDays(days))
            .Where(b => b.IsPending)
            .OrderBy(b => b.CreatedAt)
            .ToList();

        return Task.FromResult(pending);
    }

    private List<AgendaBookingResponse> InRange(Guid businessId, DateTimeOffset from, DateTimeOffset until)
    {
        EnsureSeeded(businessId);

        return _agenda
            .Where(b => b.BusinessId == businessId && b.StartsAt >= from && b.StartsAt < until)
            .Concat(_bookings
                .Where(b => b.BusinessId == businessId && b.StartsAt >= from && b.StartsAt < until)
                .Select(Project))
            .OrderBy(b => b.StartsAt)
            .ToList();
    }

    public Task<AgendaBookingResponse?> MarkBookingNoShowAsync(Guid bookingId)
    {
        var booking = _agenda.FirstOrDefault(b => b.Id == bookingId);
        if (booking is not null) booking.Status = BookingStatuses.NoShow;
        else SetStatus(bookingId, BookingStatuses.NoShow);

        return Task.FromResult<AgendaBookingResponse?>(booking);
    }

    public Task<AgendaBookingResponse?> SetCancellationReasonAsync(Guid bookingId, BookingDetails details)
    {
        var booking = _agenda.FirstOrDefault(b => b.Id == bookingId);
        if (booking is not null) booking.Details = details;

        return Task.FromResult<AgendaBookingResponse?>(booking);
    }

    public Task<AgendaBookingResponse?> SetCustomerNameAsync(Guid bookingId, string customerName)
    {
        var booking = _agenda.FirstOrDefault(b => b.Id == bookingId);
        if (booking is not null)
        {
            booking.Details = new BookingDetails
            {
                CustomerName = customerName,
                CreatedBy = "customer",
            };
        }

        return Task.FromResult<AgendaBookingResponse?>(booking);
    }

    public Task<AgendaBookingResponse?> MoveBookingAsync(
        Guid bookingId, Guid operatorId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        var booking = _agenda.FirstOrDefault(b => b.Id == bookingId);
        if (booking is not null)
        {
            booking.OperatorId = operatorId;
            booking.StartsAt = startsAt;
            booking.EndsAt = endsAt;
            booking.Operator = new AgendaOperatorRef { Id = operatorId, DisplayName = NameFor(operatorId) };
        }
        else
        {
            var customer = _bookings.FirstOrDefault(b => b.Id == bookingId);
            if (customer is not null)
            {
                customer.OperatorId = operatorId;
                customer.StartsAt = startsAt;
                customer.EndsAt = endsAt;
            }
        }

        return Task.FromResult<AgendaBookingResponse?>(booking);
    }

    public Task<AgendaBookingResponse?> CreateOperatorBookingAsync(CreateOperatorBookingRequest request)
    {
        var booking = new AgendaBookingResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            OperatorId = request.OperatorId,
            ServiceId = request.ServiceId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = request.Status,
            CreatedAt = DateTimeOffset.UtcNow,
            Note = request.Note,
            CustomerNameColumn = request.CustomerName,
            CustomerPhoneColumn = request.CustomerPhone,
            Details = request.Details,
            Operator = new AgendaOperatorRef { Id = request.OperatorId, DisplayName = NameFor(request.OperatorId) },
            Service = new AgendaServiceRef
            {
                Id = request.ServiceId,
                Name = "Booked in person",
                EstMinutes = (int)(request.EndsAt - request.StartsAt).TotalMinutes,
            },
        };

        _agenda.Add(booking);
        return Task.FromResult<AgendaBookingResponse?>(booking);
    }

    private static string NameFor(Guid operatorId) =>
        operatorId == StubOperatorService.FirstOperatorId ? "Ahmed"
        : operatorId == StubOperatorService.SecondOperatorId ? "Yusuf"
        : "Any available";

    private static AgendaBookingResponse Project(BookingResponse booking) => new()
    {
        Id = booking.Id,
        BusinessId = booking.BusinessId,
        OperatorId = booking.OperatorId == Guid.Empty ? null : (Guid?)booking.OperatorId,
        ServiceId = booking.ServiceId,
        CustomerId = booking.CustomerId,
        StartsAt = booking.StartsAt,
        EndsAt = booking.EndsAt,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProgressStatus = booking.ProgressStatus,
        Operator = booking.OperatorId == Guid.Empty
            ? null
            : new AgendaOperatorRef { Id = booking.OperatorId, DisplayName = NameFor(booking.OperatorId) },
    };

    private BookingResponse SetStatus(Guid bookingId, string status)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is not null)
        {
            booking.Status = status;
            return booking;
        }

        var agendaBooking = _agenda.FirstOrDefault(b => b.Id == bookingId);
        if (agendaBooking is not null)
            agendaBooking.Status = status;

        return new BookingResponse { Id = bookingId, Status = status };
    }

    // A day that looks like the real thing, so the agenda's rows, gaps, now line and in-chair card
    // can all be seen without a Supabase project behind them.
    private void EnsureSeeded(Guid businessId)
    {
        if (_seeded) return;
        _seeded = true;

        var today = DateTimeOffset.UtcNow.ToOffset(SastOffset).Date;

        DateTimeOffset At(int hour, int minute) => new(today.AddHours(hour).AddMinutes(minute), SastOffset);

        void Add(string name, int startHour, int startMinute, int minutes, int priceCents,
                 string service, Guid operatorId, string status, int bookedHoursAgo)
        {
            _agenda.Add(new AgendaBookingResponse
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                OperatorId = operatorId,
                ServiceId = Guid.NewGuid(),
                StartsAt = At(startHour, startMinute),
                EndsAt = At(startHour, startMinute).AddMinutes(minutes),
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-bookedHoursAgo),
                Operator = new AgendaOperatorRef { Id = operatorId, DisplayName = NameFor(operatorId) },
                Service = new AgendaServiceRef
                {
                    Id = Guid.NewGuid(),
                    Name = service,
                    PriceCents = priceCents,
                    EstMinutes = minutes,
                },
                CustomerNameColumn = name,
            });
        }

        var one = StubOperatorService.FirstOperatorId;
        var two = StubOperatorService.SecondOperatorId;

        Add("Ahmed K.", 8, 0, 60, 95000, "Minor service", one, BookingStatuses.Completed, 48);
        Add("Fatima P.", 9, 30, 45, 45000, "Brake check", two, BookingStatuses.Completed, 30);
        Add("Sipho M.", 10, 30, 90, 240000, "Major service", two, BookingStatuses.Confirmed, 26);
        Add("Naledi B.", 14, 0, 60, 95000, "Minor service", one, BookingStatuses.Confirmed, 50);
        Add("Riaz D.", 15, 30, 60, 95000, "Minor service", one, BookingStatuses.Pending, 2);
    }

    private bool _seeded;
    private readonly List<AgendaBookingResponse> _agenda = new();
    private static readonly TimeSpan SastOffset = TimeSpan.FromHours(2);

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

    public Task<UpcomingBookingResponse?> GetBookingAsync(Guid bookingId)
        => Task.FromResult<UpcomingBookingResponse?>(null);

    public Task<AgendaBookingResponse?> MarkCancelledByCustomerAsync(Guid bookingId, BookingDetails? existing)
        => Task.FromResult<AgendaBookingResponse?>(null);

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
