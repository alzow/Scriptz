using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Api.Operator;

public interface IOperatorService
{
    Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId);
}
