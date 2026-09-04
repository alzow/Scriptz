using QueueApp.Framework.Base;
using QueueApp.Services.Api;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Services.Api.Operator;

public class OperatorService : BaseService, IOperatorService
{
    private readonly IOperatorApi _api;

    public OperatorService(IOperatorApi api)
    {
        _api = api;
    }

    public Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetOperatorsAsync(PostgrestFilter.Eq(businessId)));

    public Task<List<OperatorResponse>> GetAllOperatorsForManagementAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetAllOperatorsForManagementAsync(PostgrestFilter.Eq(businessId)));

    public Task<List<OperatorResponse>> CreateOperatorAsync(CreateOperatorRequest request) =>
        ExecuteApiCallAsync(_api.CreateOperatorAsync(request));

    public Task UpdateOperatorAsync(Guid id, UpdateOperatorRequest request) =>
        ExecuteApiCallAsync(_api.UpdateOperatorAsync(PostgrestFilter.Eq(id), request));

    public Task SetOperatorActiveAsync(Guid id, bool isActive) =>
        ExecuteApiCallAsync(_api.SetOperatorActiveAsync(PostgrestFilter.Eq(id), new SetOperatorActiveRequest { IsActive = isActive }));

    public Task SetOperatorAvailableAsync(Guid id, bool isAvailable) =>
        ExecuteApiCallAsync(_api.SetOperatorAvailableAsync(PostgrestFilter.Eq(id), new SetOperatorAvailableRequest { IsAvailable = isAvailable }));

    public Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(Guid operatorId) =>
        ExecuteApiCallAsync(_api.GetAvailabilityAsync(PostgrestFilter.Eq(operatorId)));

    public Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(IReadOnlyCollection<Guid> operatorIds)
    {
        if (operatorIds.Count == 0)
            return Task.FromResult(new List<OperatorAvailabilityResponse>());

        return ExecuteApiCallAsync(_api.GetAvailabilityForOperatorsAsync(PostgrestFilter.In(operatorIds)));
    }

    public async Task<BusinessHours> GetBusinessHoursAsync(IEnumerable<OperatorResponse> operators)
    {
        var activeIds = operators.Where(o => o.IsActive).Select(o => o.Id).ToList();
        if (activeIds.Count == 0)
            return BusinessHours.Unknown;

        return BusinessHours.FromAvailability(await GetAvailabilityAsync(activeIds));
    }

    public Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request) =>
        ExecuteApiCallAsync(_api.CreateAvailabilityAsync(request));

    public Task DeleteAvailabilityAsync(Guid id) =>
        ExecuteApiCallAsync(_api.DeleteAvailabilityAsync(PostgrestFilter.Eq(id)));

    public Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(Guid operatorId) =>
        ExecuteApiCallAsync(_api.GetAvailabilityBlocksAsync(PostgrestFilter.Eq(operatorId)));

    public Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(
        IReadOnlyCollection<Guid> operatorIds, DateTimeOffset from, DateTimeOffset until)
    {
        if (operatorIds.Count == 0)
            return Task.FromResult(new List<AvailabilityBlockResponse>());

        return ExecuteApiCallAsync(_api.GetAvailabilityBlocksForOperatorsAsync(
            PostgrestFilter.In(operatorIds), PostgrestFilter.OverlapsRange(from, until)));
    }

    public Task<List<AvailabilityBlockResponse>> CreateAvailabilityBlockAsync(CreateAvailabilityBlockRequest request) =>
        ExecuteApiCallAsync(_api.CreateAvailabilityBlockAsync(request));

    public Task DeleteAvailabilityBlockAsync(Guid id) =>
        ExecuteApiCallAsync(_api.DeleteAvailabilityBlockAsync(PostgrestFilter.Eq(id)));
}
