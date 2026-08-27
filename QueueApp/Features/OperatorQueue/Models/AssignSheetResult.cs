namespace QueueApp.Features.OperatorQueue.Models;

public sealed record AssignSheetResult(bool Assigned, Guid? OperatorId, bool MarkNoShow)
{
    public static readonly AssignSheetResult Dismissed = new(false, null, false);
}
