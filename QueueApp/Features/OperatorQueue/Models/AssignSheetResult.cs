namespace QueueApp.Features.OperatorQueue.Models;

public sealed record AssignSheetResult(bool Assigned, Guid? OperatorId)
{
    public static readonly AssignSheetResult Dismissed = new(false, null);
}
