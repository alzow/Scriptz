namespace QueueApp.Features.Flow.Visit.Models;

public enum VisitOption
{
    Dismissed,
    Call,
    Directions,
    Share,
    AddToCalendar,
    GoAgain,
    LeaveQueue,
    CancelBooking,
}

public sealed record VisitOptionResult(VisitOption Option);
