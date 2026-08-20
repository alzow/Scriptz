using Refit;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Api.Operator;

public interface IOperatorApi
{
    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    // Filtered to active operators only — deactivated staff must not appear as a chair on the live queue.
    [Get("/operators")]
    Task<List<OperatorResponse>> GetOperatorsAsync(
        [AliasAs("business_id")] string businessIdEq,
        [AliasAs("is_active")] string isActiveEq = "eq.true",
        [AliasAs("order")] string order = "sort_order.asc");

    // For the management screen — includes inactive operators so they can be reactivated.
    [Get("/operators")]
    Task<List<OperatorResponse>> GetAllOperatorsForManagementAsync(
        [AliasAs("business_id")] string businessIdEq,
        [AliasAs("order")] string order = "sort_order.asc");

    [Post("/operators")]
    Task<List<OperatorResponse>> CreateOperatorAsync([Body] CreateOperatorRequest request);

    [Patch("/operators")]
    Task UpdateOperatorAsync([AliasAs("id")] string idEq, [Body] UpdateOperatorRequest request);

    [Patch("/operators")]
    Task SetOperatorActiveAsync([AliasAs("id")] string idEq, [Body] SetOperatorActiveRequest request);

    [Get("/operator_availability")]
    Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(
        [AliasAs("operator_id")] string operatorIdEq,
        [AliasAs("order")] string order = "day_of_week.asc,start_time.asc");

    [Post("/operator_availability")]
    Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync([Body] CreateAvailabilityRequest request);

    [Delete("/operator_availability")]
    Task DeleteAvailabilityAsync([AliasAs("id")] string idEq);
}
