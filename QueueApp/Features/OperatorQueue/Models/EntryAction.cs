namespace QueueApp.Features.OperatorQueue.Models;

public enum EntryAction
{
    Dismissed,
    ServeNow,
    MoveToAnotherOperator,
    MoveToEndOfQueue,
    ChangeService,
    SaveNote,
    MarkNoShow,
    RemoveFromQueue,
}
