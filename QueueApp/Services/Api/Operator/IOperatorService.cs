using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Api.Operator;

public interface IOperatorService
{
    Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId);
    Task<List<OperatorResponse>> GetAllOperatorsForManagementAsync(Guid businessId);
    Task<List<OperatorResponse>> CreateOperatorAsync(CreateOperatorRequest request);
    Task UpdateOperatorAsync(Guid id, UpdateOperatorRequest request);
    Task SetOperatorActiveAsync(Guid id, bool isActive);

    Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(Guid operatorId);
    Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request);
    Task DeleteAvailabilityAsync(Guid id);

    Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(Guid operatorId);
    Task<List<AvailabilityBlockResponse>> CreateAvailabilityBlockAsync(CreateAvailabilityBlockRequest request);
    Task DeleteAvailabilityBlockAsync(Guid id);
}
