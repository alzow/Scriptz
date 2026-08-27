namespace QueueApp.Features.BookingAgenda.Models;

public enum BookingAction
{
    Dismissed,
    Start,
    Complete,
    MoveToAnotherTime,
    MoveToResource,
    MarkNoShow,
    Cancel,
    SaveProgress,
}
