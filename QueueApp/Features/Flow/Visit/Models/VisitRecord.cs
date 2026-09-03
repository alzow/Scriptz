using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.Flow.Visit.Models;

public enum VisitKind
{
    Queue,
    Booking,
}

public enum VisitLifecycle
{
    Live,
    AwaitingCollection,
    Settled,
    Cancelled,
    NoShow,
}

public sealed class VisitRecord
{
    public required VisitKind Kind { get; init; }
    public required Guid Id { get; init; }
    public required Guid BusinessId { get; init; }
    public required string BusinessName { get; init; }
    public required VisitLifecycle Lifecycle { get; init; }
    public required string StatusText { get; init; }
    public required string ServiceName { get; init; }
    public required string OperatorName { get; init; }

    // False only when nobody was on shift to assign (queue), or the shop hasn't picked who is
    // taking it yet (booking). Everything phrased in an operator's terms has to check this first.
    public required bool HasOperator { get; init; }
    // Queue only: the booking reads embed the operator by display name alone, and nothing on the
    // booking side needs the id.
    public Guid? OperatorId { get; init; }

    // Queue only. Carried so leaving the queue can write its stamp without dropping whatever else
    // the entry's details already hold.
    public QueueEntryDetails? Details { get; init; }
    public required string PriceText { get; init; }

    // What the customer answered before the entry existed, in the order the questions were asked.
    // Each answer carries its own label and type, so this renders without ever reading the field
    // definitions — a question renamed or deleted since does not rewrite what was already answered.
    public IReadOnlyList<IntakeAnswer> IntakeAnswers { get; init; } = Array.Empty<IntakeAnswer>();

    public string? ShopUpdate { get; init; }
    public string? CustomerNote { get; init; }
    public string? ShopNote { get; init; }
    public string? CancellationReason { get; init; }
    public bool CancelledByCustomer { get; init; }
    public bool CancelledByShop { get; init; }

    public DateTimeOffset? RequestedAt { get; init; }
    public DateTimeOffset? JoinedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public DateTimeOffset? AwaitingCollectionAt { get; init; }
    public DateTimeOffset? CollectedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public DateTimeOffset? SlotStart { get; init; }
    public DateTimeOffset? SlotEnd { get; init; }

    public int Position { get; set; }
    public decimal? WaitMinutes { get; set; }

    public bool IsQueue => Kind == VisitKind.Queue;
    public bool IsBooking => Kind == VisitKind.Booking;

    public bool IsLive => Lifecycle is VisitLifecycle.Live or VisitLifecycle.AwaitingCollection;
    public bool IsAwaitingCollection => Lifecycle == VisitLifecycle.AwaitingCollection;
    public bool IsSettled => Lifecycle == VisitLifecycle.Settled;
    public bool WasCancelled => Lifecycle == VisitLifecycle.Cancelled;
    public bool WasNoShow => Lifecycle == VisitLifecycle.NoShow;
    public bool HasPrice => !string.IsNullOrWhiteSpace(PriceText);
    public bool HasIntakeAnswers => IntakeAnswers.Count > 0;
    public bool HasCustomerNote => !string.IsNullOrWhiteSpace(CustomerNote);
    public bool HasShopNote => !string.IsNullOrWhiteSpace(ShopNote) || !string.IsNullOrWhiteSpace(ShopUpdate);
    public bool HasCancellationReason => !string.IsNullOrWhiteSpace(CancellationReason);
    public bool IsBeingServed => IsQueue && IsLive && StartedAt is not null;

    public string ShopNoteText => string.IsNullOrWhiteSpace(ShopNote) ? ShopUpdate ?? string.Empty : ShopNote;

    public TimeSpan? Waited => JoinedAt is { } joined && StartedAt is { } started && started > joined
        ? started - joined
        : null;

    public TimeSpan? Served => StartedAt is { } started && FinishedAt is { } finished && finished > started
        ? finished - started
        : null;

    public static VisitRecord FromEntry(MyQueueEntryResponse entry)
    {
        var lifecycle = ResolveEntryLifecycle(entry);

        return new VisitRecord
        {
            Kind = VisitKind.Queue,
            Id = entry.Id,
            BusinessId = entry.BusinessId,
            BusinessName = entry.BusinessName,
            Lifecycle = lifecycle,
            StatusText = EntryStatusText(entry, lifecycle),
            ServiceName = string.IsNullOrWhiteSpace(entry.ServiceName) ? "Not recorded" : entry.ServiceName,
            OperatorName = entry.OperatorName,
            HasOperator = entry.HasOperator,
            OperatorId = entry.OperatorId,
            Details = entry.Details,
            IntakeAnswers = OrderAnswers(entry.IntakeResponses),
            PriceText = MoneyFormat.Format(entry.PriceCents),
            ShopUpdate = entry.ProgressStatus,
            ShopNote = entry.Note,
            CancelledByCustomer = entry.Details?.CancelledBy == CancelledByValues.Customer,
            CancelledByShop = entry.Details?.CancelledBy == CancelledByValues.Business,
            JoinedAt = entry.JoinedAtUtc,
            StartedAt = entry.ServingAtUtc,
            FinishedAt = entry.DoneAtUtc,
            AwaitingCollectionAt = entry.AwaitingCollectionAtUtc,
            CollectedAt = entry.CollectedAtUtc,
            CancelledAt = entry.CancelledAtUtc,
        };
    }

    public static VisitRecord FromBooking(UpcomingBookingResponse booking)
    {
        var lifecycle = ResolveBookingLifecycle(booking);

        return new VisitRecord
        {
            Kind = VisitKind.Booking,
            Id = booking.Id,
            BusinessId = booking.BusinessId,
            BusinessName = booking.BusinessName,
            Lifecycle = lifecycle,
            StatusText = BookingStatusText(booking, lifecycle),
            ServiceName = string.IsNullOrWhiteSpace(booking.ServiceName) ? "Not recorded" : booking.ServiceName,
            OperatorName = booking.OperatorName,
            HasOperator = booking.Operator is not null,
            PriceText = booking.PriceText,
            IntakeAnswers = OrderAnswers(booking.IntakeResponses),
            ShopUpdate = booking.ProgressStatus,
            CustomerNote = booking.Note,
            CancellationReason = booking.CancellationReason,
            CancelledByCustomer = booking.CancelledBy == CancelledByValues.Customer,
            CancelledByShop = booking.CancelledBy == CancelledByValues.Business || booking.HasCancellationReason,
            RequestedAt = booking.CreatedAt == default ? null : booking.CreatedAt,
            StartedAt = booking.StartedAt,
            AwaitingCollectionAt = booking.AwaitingCollectionAt,
            CollectedAt = booking.CollectedAt,
            CancelledAt = booking.CancelledAt,
            SlotStart = booking.StartsAt,
            SlotEnd = booking.EndsAt,
        };
    }

    // The jsonb is an object keyed by field id, so it comes back in whatever order it was stored
    // in. sort_order is the order the shop chose, and it travelled with each answer for this.
    public static IReadOnlyList<IntakeAnswer> OrderAnswers(Dictionary<string, IntakeAnswer>? responses) =>
        responses is null or { Count: 0 }
            ? Array.Empty<IntakeAnswer>()
            : responses.Values.OrderBy(a => a.SortOrder).ToList();

    public static VisitLifecycle ResolveEntryLifecycle(MyQueueEntryResponse entry)
    {
        if (entry.IsNoShow)
            return VisitLifecycle.NoShow;

        if (entry.IsCancelled)
            return VisitLifecycle.Cancelled;

        if (entry.IsAwaitingCollection)
            return VisitLifecycle.AwaitingCollection;

        if (entry.IsFinished)
            return VisitLifecycle.Settled;

        return entry.IsLive ? VisitLifecycle.Live : VisitLifecycle.Settled;
    }

    public static VisitLifecycle ResolveBookingLifecycle(UpcomingBookingResponse booking)
    {
        if (booking.Status == BookingStatuses.NoShow)
            return VisitLifecycle.NoShow;

        if (booking.Status == BookingStatuses.Cancelled)
            return VisitLifecycle.Cancelled;

        if (booking.IsAwaitingCollection)
            return VisitLifecycle.AwaitingCollection;

        if (booking.Status is BookingStatuses.Pending or BookingStatuses.Confirmed or BookingStatuses.InProgress)
            return booking.EndsAt > DateTimeOffset.UtcNow ? VisitLifecycle.Live : VisitLifecycle.Settled;

        return VisitLifecycle.Settled;
    }

    public static string EntryStatusText(MyQueueEntryResponse entry, VisitLifecycle lifecycle) => lifecycle switch
    {
        VisitLifecycle.NoShow => "NO-SHOW",
        VisitLifecycle.Cancelled => entry.Details?.CancelledBy == CancelledByValues.Customer
            ? "YOU LEFT"
            : "CANCELLED",
        VisitLifecycle.AwaitingCollection => "READY FOR COLLECTION",
        VisitLifecycle.Live => entry.IsBeingServed ? "IN THE CHAIR" : "IN THE QUEUE",
        _ => "SERVED",
    };

    public static string BookingStatusText(UpcomingBookingResponse booking, VisitLifecycle lifecycle) => lifecycle switch
    {
        VisitLifecycle.NoShow => "NO-SHOW",
        VisitLifecycle.Cancelled => booking.WasCancelledByCustomer ? "YOU CANCELLED" : "CANCELLED",
        VisitLifecycle.AwaitingCollection => "READY FOR COLLECTION",
        VisitLifecycle.Live => booking.Status == BookingStatuses.Pending ? "PENDING" : "CONFIRMED",
        _ => booking.Status == BookingStatuses.Completed ? "COMPLETED" : "SLOT PASSED",
    };
}
