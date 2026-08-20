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

    public Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(Guid operatorId) =>
        ExecuteApiCallAsync(_api.GetAvailabilityAsync($"eq.{operatorId}"));

    public Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request) =>
        ExecuteApiCallAsync(_api.CreateAvailabilityAsync(request));

    public Task DeleteAvailabilityAsync(Guid id) =>
        ExecuteApiCallAsync(_api.DeleteAvailabilityAsync($"eq.{id}"));
}
