namespace QueueApp.Features.OperatorQueue.Models;

public static class BoardConstants
{
    public const int PoolStarvationMinutes = 10;

    public const int TickIntervalSeconds = 1;
    public const int HeartbeatTicks = 120;

    public const int MinimumAverageSamples = 3;

    public const string EmDash = "—";

    public const string WalkInName = "Walk-in";

    public const string StartServingAction = "Start serving";
    public const string MarkDoneAction = "Mark done";
    public const string ReadyForCollectionAction = "Ready for collection";
    public const string AssignHeader = "WHO'S TAKING THIS ONE?";
    public const string MoveHeader = "MOVE TO";

    public const string LeaveNoteHeader = "LEAVE A NOTE";
    public const string NoteHeader = "NOTE";
    public const string OneAnswerSummary = "Customer answered 1 question";
    public const string ManyAnswersSummary = "Customer answered {0} questions";

    // start_serving on a pooled board picks a free resource itself and refuses when there is none.
    // That is the shop being busy, not the app failing, so it is not titled as a fault.
    public const string CantStartTitle = "Can't start yet";

    public const string NoServicesTitle = "No services yet";
    public const string NoServicesMessage = "Add a service under Settings before adding anyone to the queue.";

    public const string NoShowTitle = "No-show";
    public const string RemoveTitle = "Remove from queue";
    public const string RemoveConfirm = "Remove";
    public const string RemoveCancel = "Keep";

    public static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
        : value.ToUniversalTime();
}
