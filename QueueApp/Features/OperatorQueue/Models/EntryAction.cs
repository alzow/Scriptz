namespace QueueApp.Features.OperatorQueue.Models;

// What the row actions sheet came back with. The two destructive members live below the sheet's
// separator and nowhere else: a mis-tapped no-show ejects someone who has physically stood there
// for fourteen minutes, with no undo.
public enum EntryAction
{
    Dismissed,
    ServeNow,
    MoveToAnotherOperator,
    MoveToEndOfQueue,
    ChangeService,
    MarkNoShow,
    RemoveFromQueue,
}
