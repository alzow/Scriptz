namespace QueueApp.Features.OperatorQueue.Models;

// OperatorId is nullable because "back to the shared pool" is a destination the sheet offers,
// not the absence of a choice — Assigned tells the two apart.
public sealed record AssignSheetResult(bool Assigned, Guid? OperatorId, bool MarkNoShow)
{
    public static readonly AssignSheetResult Dismissed = new(false, null, false);
}
