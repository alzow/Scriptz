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

    // Optional, quiet staff-facing note on a serving entry — "finishing interior", etc.
    [Post("/rpc/set_queue_progress")]
    Task<QueueEntryResponse> SetQueueProgressAsync([Body] SetProgressRequest request);

    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    [Get("/queue_entries")]
    Task<List<QueueEntryResponse>> GetActiveEntriesAsync(
        [AliasAs("business_id")] string businessIdEq,
        [AliasAs("status")] string statusEq = "in.(waiting,serving)",
        [AliasAs("order")] string order = "joined_at.asc");

    [Post("/rpc/business_queue_summary")]
    Task<List<QueueSummaryRow>> GetQueueSummaryAsync([Body] BusinessIdRequest request);

    // Rolling completed-service average, used for the shop stats "Avg" tile. Returns null until the
    // operator has enough history (the function carries a count(*) >= 3 guard), which the board
    // renders as an em-dash rather than inventing a number.
    [Post("/rpc/operator_avg_minutes")]
    Task<decimal?> GetOperatorAvgMinutesAsync([Body] OperatorAvgRequest request);

    // Direct column writes the queue engine has no RPC for: assigning an entry to (or back off) an
    // operator, sending one to the back of the queue, and correcting the service on an existing
    // entry. Permitted by the "owner or self manage" UPDATE policy on queue_entries.
    [Patch("/queue_entries")]
    Task UpdateEntryAsync([AliasAs("id")] string idEq, [Body] Dictionary<string, object?> patch);

    // "Done today" for the shop stats. Both enum spellings are accepted because the app has used
    // "completed" and "done" interchangeably; PostgREST ignores labels the enum doesn't define.
    [Get("/queue_entries")]
    Task<List<QueueEntryResponse>> GetCompletedSinceAsync(
        [AliasAs("business_id")] string businessIdEq,
        [AliasAs("done_at")] string doneAtGte,
        [AliasAs("status")] string statusIn = "in.(done,completed)",
        [AliasAs("select")] string select = "id");

    [Get("/visits?select=id,visited_at,business:businesses(id,name,category),operator:operators(display_name),service:services(name)&order=visited_at.desc")]
    Task<List<VisitResponse>> GetMyVisitsAsync([AliasAs("customer_id")] string customerIdEq);
}
