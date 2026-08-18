using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

public interface IQueueService
{
    Task<Guid> GetOwnedBusinessIdAsync();
    Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId);
    Task<List<QueueEntryResponse>> GetWaitingAsync(Guid businessId);
    Task AddWalkInAsync(Guid businessId, Guid? operatorId, string name);
    Task StartServingAsync(Guid entryId);
    Task CompleteAsync(Guid entryId);
    Task NoShowAsync(Guid entryId);
    Task HeartbeatAsync(Guid businessId);
}
