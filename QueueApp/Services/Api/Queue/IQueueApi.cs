using Refit;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

public interface IQueueApi
{
    // Engine RPC calls
    [Post("/rpc/join_queue")]
    Task JoinQueueAsync([Body] JoinQueueRequest request);

    [Post("/rpc/start_serving")]
    Task StartServingAsync([Body] EntryIdRequest request);

    [Post("/rpc/complete_entry")]
    Task CompleteEntryAsync([Body] EntryIdRequest request);

    [Post("/rpc/mark_no_show")]
    Task MarkNoShowAsync([Body] EntryIdRequest request);

    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    [Get("/queue_entries")]
    Task<List<QueueEntryResponse>> GetWaitingAsync(
        [AliasAs("business_id")] string businessIdEq,
        [AliasAs("status")] string statusEq = "eq.waiting",
        [AliasAs("order")] string order = "joined_at.asc");

    [Post("/rpc/business_queue_summary")]
    Task<List<QueueSummaryRow>> GetQueueSummaryAsync([Body] BusinessIdRequest request);
}
