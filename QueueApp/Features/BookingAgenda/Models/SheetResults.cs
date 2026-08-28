namespace QueueApp.Features.BookingAgenda.Models;

public sealed record BookingActionResult(
    BookingAction Action,
    Guid? OperatorId = null,
    string? ProgressStatus = null);

public sealed record MoveBookingResult(
    bool Confirmed,
    Guid OperatorId = default,
    DateTimeOffset StartsAt = default,
    DateTimeOffset EndsAt = default);

public sealed record BlockTimeResult(
    bool Confirmed,
    IReadOnlyList<Guid>? OperatorIds = null,
    DateTimeOffset StartsAt = default,
    DateTimeOffset EndsAt = default,
    string? Reason = null);
