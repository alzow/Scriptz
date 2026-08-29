using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Api.Operator;

public interface IOperatorService
{
    Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId);
    Task<List<OperatorResponse>> GetAllOperatorsForManagementAsync(Guid businessId);
    Task<List<OperatorResponse>> CreateOperatorAsync(CreateOperatorRequest request);
    Task UpdateOperatorAsync(Guid id, UpdateOperatorRequest request);
    Task SetOperatorActiveAsync(Guid id, bool isActive);

    // Shift toggle for the operator board's off-shift row. Separate from SetOperatorActiveAsync:
    // going off shift for the afternoon must not read as being taken off the roster.
    Task SetOperatorAvailableAsync(Guid id, bool isAvailable);

    Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(Guid operatorId);
    Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(IReadOnlyCollection<Guid> operatorIds);
    Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request);
    Task DeleteAvailabilityAsync(Guid id);

    Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(Guid operatorId);
    Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(
        IReadOnlyCollection<Guid> operatorIds, DateTimeOffset from, DateTimeOffset until);
    Task<List<AvailabilityBlockResponse>> CreateAvailabilityBlockAsync(CreateAvailabilityBlockRequest request);
    Task DeleteAvailabilityBlockAsync(Guid id);
}
