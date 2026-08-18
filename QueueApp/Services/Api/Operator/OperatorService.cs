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
}
