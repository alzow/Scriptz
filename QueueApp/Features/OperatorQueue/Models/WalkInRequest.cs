namespace QueueApp.Features.OperatorQueue.Models;

public sealed record WalkInRequest(Guid? OperatorId, string? Name, Guid ServiceId);
