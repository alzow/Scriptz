using QueueApp.Framework.Base;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Api.Queue;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class QueueService : BaseService, IQueueService
{
    private readonly IQueueApi _api;
    private readonly IAuthService _authService;

    public QueueService(IQueueApi api, IAuthService authService)
    {
        _api = api;
        _authService = authService;
    }

    public async Task<Guid> GetOwnedBusinessIdAsync()
    {
        var userId = await _authService.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("No authenticated user is available for queue ownership lookup.");

        var businesses = await ExecuteApiCallAsync(_api.GetOwnedBusinessesAsync($"eq.{userId}", "id"));
        var business = businesses.FirstOrDefault();
        if (business is null)
            throw new InvalidOperationException("No business is associated with the current user.");

        return business.Id;
    }

    public async Task<BusinessResponse?> GetBusinessAsync(Guid businessId)
    {
        var businesses = await ExecuteApiCallAsync(_api.GetBusinessesAsync($"eq.{businessId}"));
        return businesses.FirstOrDefault();
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
