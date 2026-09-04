namespace QueueApp.Features.OperatorQueue.Models;

public enum EntryAction
{
    Dismissed,
    ServeNow,
    MarkDone,
    MoveToAnotherOperator,
    MoveToEndOfQueue,
    ChangeService,
    SaveNote,
    MarkNoShow,
    RemoveFromQueue,
    ViewAnswers,
}
