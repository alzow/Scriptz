namespace QueueApp.Features.BookingAgenda.Models;

public enum BookingAction
{
    Dismissed,
    Complete,
    MoveToAnotherTime,
    MoveToResource,
    MarkNoShow,
    Cancel,
    SaveProgress,
}
