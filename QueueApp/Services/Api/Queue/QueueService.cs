using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Api.Queue.Models;

namespace ScriptzApp.Services.Api.Queue;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class QueueService : BaseService, IQueueService
{
    private readonly IQueueApi _api;

    public QueueService(IQueueApi api)
    {
        _api = api;
    }

    public Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetOperatorsAsync($"eq.{businessId}"));

    public Task<List<QueueEntryResponse>> GetWaitingAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetWaitingAsync($"eq.{businessId}"));

    public Task AddWalkInAsync(Guid businessId, Guid? operatorId, string name) =>
        ExecuteApiCallAsync(_api.JoinQueueAsync(new JoinQueueRequest
        {
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerName = name,
        }));

    public Task StartServingAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.StartServingAsync(new EntryIdRequest { EntryId = entryId }));

    public Task CompleteAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.CompleteEntryAsync(new EntryIdRequest { EntryId = entryId }));

    public Task NoShowAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.MarkNoShowAsync(new EntryIdRequest { EntryId = entryId }));

    public Task HeartbeatAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.HeartbeatAsync($"eq.{businessId}",
            new Dictionary<string, object> { ["last_seen_at"] = DateTime.UtcNow }));
}
