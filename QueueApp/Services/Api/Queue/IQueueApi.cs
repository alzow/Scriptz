using Refit;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

public interface IQueueApi
{
    // Engine RPC calls
    [Post("/rpc/join_queue")]
    Task<QueueEntryResponse> JoinQueueAsync([Body] JoinQueueRequest request);

    [Post("/rpc/cancel_entry")]
    Task<QueueEntryResponse> CancelEntryAsync([Body] EntryIdRequest request);

    [Post("/rpc/start_serving")]
    Task StartServingAsync([Body] EntryIdRequest request);

    [Post("/rpc/complete_entry")]
    Task CompleteEntryAsync([Body] EntryIdRequest request);

    [Post("/rpc/mark_no_show")]
    Task MarkNoShowAsync([Body] EntryIdRequest request);

    [Post("/rpc/my_queue_status")]
    Task<List<MyQueueStatusResponse>> GetMyQueueStatusAsync([Body] BusinessIdRequest request);

    // No business_id — the Browse dashboard doesn't know where the customer is queued.
    [Post("/rpc/my_active_queue_entry")]
    Task<List<MyActiveQueueEntryResponse>> GetMyActiveEntryAsync();

    [Post("/rpc/queue_entry_wait_minutes")]
    Task<decimal?> GetEntryWaitMinutesAsync([Body] QueueEntryIdRequest request);

    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    [Get("/queue_entries")]
    Task<List<QueueEntryResponse>> GetActiveEntriesAsync(
        [AliasAs("business_id")] string businessIdEq,
        [AliasAs("status")] string statusEq = "in.(waiting,serving)",
        [AliasAs("order")] string order = "joined_at.asc");

    [Post("/rpc/business_queue_summary")]
    Task<List<QueueSummaryRow>> GetQueueSummaryAsync([Body] BusinessIdRequest request);

    [Get("/visits?select=id,visited_at,business:businesses(id,name),operator:operators(display_name),service:services(name)&order=visited_at.desc")]
    Task<List<VisitResponse>> GetMyVisitsAsync([AliasAs("customer_id")] string customerIdEq);
}
