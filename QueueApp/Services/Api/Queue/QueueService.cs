using QueueApp.Framework.Base;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class QueueService : BaseService, IQueueService
{
    private readonly IQueueApi _api;

    public QueueService(IQueueApi api)
    {
        _api = api;
    }

    public Task<List<QueueEntryResponse>> GetActiveEntriesAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetActiveEntriesAsync($"eq.{businessId}"));

    public Task AddWalkInAsync(Guid businessId, Guid? operatorId, string name, Guid serviceId) =>
        ExecuteApiCallAsync(_api.JoinQueueAsync(new JoinQueueRequest
        {
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerName = name,
            ServiceId = serviceId,
        }));

    public Task StartServingAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.StartServingAsync(new EntryIdRequest { EntryId = entryId }));

    public Task CompleteAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.CompleteEntryAsync(new EntryIdRequest { EntryId = entryId }));

    public Task NoShowAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.MarkNoShowAsync(new EntryIdRequest { EntryId = entryId }));

    public Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetQueueSummaryAsync(new BusinessIdRequest { BusinessId = businessId }));

    public Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId, string? customerName, Guid serviceId) =>
        ExecuteApiCallAsync(_api.JoinQueueAsync(new JoinQueueRequest
        {
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerId = customerId,
            CustomerName = customerName,
            ServiceId = serviceId,
        }));

    public Task<QueueEntryResponse> CancelEntryAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.CancelEntryAsync(new EntryIdRequest { EntryId = entryId }));

    public async Task<MyQueueStatusResponse?> GetMyQueueStatusAsync(Guid businessId)
    {
        var results = await ExecuteApiCallAsync(_api.GetMyQueueStatusAsync(new BusinessIdRequest { BusinessId = businessId }));
        return results.FirstOrDefault();
    }

    public async Task<MyActiveQueueEntryResponse?> GetMyActiveEntryAsync()
    {
        var results = await ExecuteApiCallAsync(_api.GetMyActiveEntryAsync());
        return results.FirstOrDefault();
    }

    public Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.GetEntryWaitMinutesAsync(new QueueEntryIdRequest { EntryId = entryId }));

    public Task<QueueEntryResponse> SetQueueProgressAsync(Guid entryId, string? status) =>
        ExecuteApiCallAsync(_api.SetQueueProgressAsync(new SetProgressRequest { EntryId = entryId, Status = status }));

    public Task<List<VisitResponse>> GetMyVisitsAsync(Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyVisitsAsync($"eq.{customerId}"));
}
