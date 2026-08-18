using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

public interface IQueueService
{
    Task<List<QueueEntryResponse>> GetWaitingAsync(Guid businessId);
    Task AddWalkInAsync(Guid businessId, Guid? operatorId, string name);
    Task StartServingAsync(Guid entryId);
    Task CompleteAsync(Guid entryId);
    Task NoShowAsync(Guid entryId);
    Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId);
}
