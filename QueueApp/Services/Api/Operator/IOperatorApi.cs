using Refit;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Api.Operator;

public interface IOperatorApi
{
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

    [Patch("/operators")]
    Task SetOperatorAvailableAsync([AliasAs("id")] string idEq, [Body] SetOperatorAvailableRequest request);

    [Get("/operator_availability")]
    Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(
        [AliasAs("operator_id")] string operatorIdEq,
        [AliasAs("order")] string order = "day_of_week.asc,start_time.asc");

    // Every window across a set of resources in one request. The trading-hours union needs all of
    // them and does not care which operator each came from, so fanning out one request per operator
    // was N round trips for a single answer.
    [Get("/operator_availability")]
    Task<List<OperatorAvailabilityResponse>> GetAvailabilityForOperatorsAsync(
        [AliasAs("operator_id")] string operatorIdIn,
        [AliasAs("order")] string order = "day_of_week.asc,start_time.asc");

    [Post("/operator_availability")]
    Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync([Body] CreateAvailabilityRequest request);

    [Delete("/operator_availability")]
    Task DeleteAvailabilityAsync([AliasAs("id")] string idEq);

    [Get("/availability_blocks")]
    Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(
        [AliasAs("operator_id")] string operatorIdEq,
        [AliasAs("order")] string order = "starts_at.asc");

    // Every block across a set of resources that overlaps a window — what the agenda needs to draw
    // blocked ranges as rows, and what the requests banner checks a pending booking against.
    // Filtered by an explicit operator id list rather than an embedded operators.business_id filter:
    // the caller already holds the business's operators, and this keeps the query shape plain.
    [Get("/availability_blocks")]
    Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksForOperatorsAsync(
        [AliasAs("operator_id")] string operatorIdIn,
        [AliasAs("and")] string overlapFilter,
        [AliasAs("order")] string order = "starts_at.asc");

    [Post("/availability_blocks")]
    Task<List<AvailabilityBlockResponse>> CreateAvailabilityBlockAsync([Body] CreateAvailabilityBlockRequest request);

    [Delete("/availability_blocks")]
    Task DeleteAvailabilityBlockAsync([AliasAs("id")] string idEq);
}
