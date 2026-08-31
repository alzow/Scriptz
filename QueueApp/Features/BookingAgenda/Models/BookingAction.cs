namespace QueueApp.Features.BookingAgenda.Models;

public enum BookingAction
{
    Dismissed,
    Confirm,
    Decline,
    Complete,
    MoveToAnotherTime,
    MoveToResource,
    MarkNoShow,
    Cancel,
    SaveProgress,
}
