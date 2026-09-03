namespace QueueApp.Features.BookingAgenda.Models;

public enum BookingAction
{
    Dismissed,
    Confirm,
    Decline,
    Complete,
    MarkCollected,
    MoveToAnotherTime,
    MoveToResource,
    MarkNoShow,
    Cancel,
    SaveProgress,
}
