using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Api.Operator;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class OperatorService : BaseService, IOperatorService
{
    private readonly IOperatorApi _api;

    public OperatorService(IOperatorApi api)
    {
        _api = api;
    }

    public Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetOperatorsAsync($"eq.{businessId}"));

    public Task<List<OperatorResponse>> GetAllOperatorsForManagementAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetAllOperatorsForManagementAsync($"eq.{businessId}"));

    public Task<List<OperatorResponse>> CreateOperatorAsync(CreateOperatorRequest request) =>
        ExecuteApiCallAsync(_api.CreateOperatorAsync(request));

    public Task UpdateOperatorAsync(Guid id, UpdateOperatorRequest request) =>
        ExecuteApiCallAsync(_api.UpdateOperatorAsync($"eq.{id}", request));

    public Task SetOperatorActiveAsync(Guid id, bool isActive) =>
        ExecuteApiCallAsync(_api.SetOperatorActiveAsync($"eq.{id}", new SetOperatorActiveRequest { IsActive = isActive }));

    public Task SetOperatorAvailableAsync(Guid id, bool isAvailable) =>
        ExecuteApiCallAsync(_api.SetOperatorAvailableAsync($"eq.{id}", new SetOperatorAvailableRequest { IsAvailable = isAvailable }));

    public Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(Guid operatorId) =>
        ExecuteApiCallAsync(_api.GetAvailabilityAsync($"eq.{operatorId}"));

    public Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(IReadOnlyCollection<Guid> operatorIds)
    {
        if (operatorIds.Count == 0)
            return Task.FromResult(new List<OperatorAvailabilityResponse>());

        return ExecuteApiCallAsync(
            _api.GetAvailabilityForOperatorsAsync($"in.({string.Join(',', operatorIds)})"));
    }

    public Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request) =>
        ExecuteApiCallAsync(_api.CreateAvailabilityAsync(request));

    public Task DeleteAvailabilityAsync(Guid id) =>
        ExecuteApiCallAsync(_api.DeleteAvailabilityAsync($"eq.{id}"));

    public Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(Guid operatorId) =>
        ExecuteApiCallAsync(_api.GetAvailabilityBlocksAsync($"eq.{operatorId}"));

    public Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(
        IReadOnlyCollection<Guid> operatorIds, DateTimeOffset from, DateTimeOffset until)
    {
        if (operatorIds.Count == 0)
            return Task.FromResult(new List<AvailabilityBlockResponse>());

        var ids = string.Join(',', operatorIds);

        // Overlap, not containment: a block that started yesterday and runs through this morning
        // still blocks this morning.
        var overlap = $"(starts_at.lt.{until:yyyy-MM-ddTHH:mm:sszzz},ends_at.gt.{from:yyyy-MM-ddTHH:mm:sszzz})";

        return ExecuteApiCallAsync(_api.GetAvailabilityBlocksForOperatorsAsync($"in.({ids})", overlap));
    }

    public Task<List<AvailabilityBlockResponse>> CreateAvailabilityBlockAsync(CreateAvailabilityBlockRequest request) =>
        ExecuteApiCallAsync(_api.CreateAvailabilityBlockAsync(request));

    public Task DeleteAvailabilityBlockAsync(Guid id) =>
        ExecuteApiCallAsync(_api.DeleteAvailabilityBlockAsync($"eq.{id}"));
}
