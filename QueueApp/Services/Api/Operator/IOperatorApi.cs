using Refit;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Api.Operator;

public interface IOperatorApi
{
    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    [Get("/operators")]
    Task<List<OperatorResponse>> GetOperatorsAsync(
        [AliasAs("business_id")] string businessIdEq,
        [AliasAs("order")] string order = "sort_order.asc");
}
