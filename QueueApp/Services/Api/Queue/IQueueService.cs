using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

public interface IQueueService
{
    Task<List<QueueEntryResponse>> GetActiveEntriesAsync(Guid businessId);
    // The shop writing an entry for someone with no account. Same RPC the customer's own join goes
    // through — same duplicate guard, same pooled-resolution of a null operator id, same intake
    // validation — with p_customer_id left off and the name typed in at the counter instead.
    Task JoinQueueAsOperatorAsync(Guid businessId, Guid? operatorId, string? customerName, Guid serviceId,
        Dictionary<string, IntakeAnswer>? intakeResponses = null);
    Task StartServingAsync(Guid entryId);
    Task CompleteAsync(Guid entryId);

    // TODO: PATCH-based by design (see Documentation/awaiting-collection-backend-requirements.md
    // §4) — no state-machine enforcement. Revisit with a dedicated RPC if that becomes a problem.
    Task MarkAwaitingCollectionAsync(Guid entryId);
    Task MarkCollectedAsync(Guid entryId);

    Task NoShowAsync(Guid entryId);
    Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId);
    // intakeResponses is null for every service that asks nothing, which leaves the request body
    // exactly as it was — see JoinQueueRequest.
    Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId, string? customerName, Guid serviceId,
        Dictionary<string, IntakeAnswer>? intakeResponses = null);
    Task<QueueEntryResponse> CancelEntryAsync(Guid entryId);
    Task<MyQueueStatusResponse?> GetMyQueueStatusAsync(Guid businessId);
    Task<MyActiveQueueEntryResponse?> GetMyActiveEntryAsync();
    Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId);
    Task<QueueEntryResponse> SetQueueProgressAsync(Guid entryId, string? status);
    // The customer's own queue entries, newest first — served, cancelled, no-showed and still
    // live alike. Replaces the `visits` read: a visits row has a visited_at and nothing else, so
    // no page built on it can say how long anyone waited.
    Task<List<MyQueueEntryResponse>> GetMyEntriesAsync(Guid customerId);
    Task<MyQueueEntryResponse?> GetEntryAsync(Guid entryId);

    // cancel_entry takes no "who", so the customer stamps their own leaving into details. The
    // details already on the entry come along because the write replaces the whole column.
    Task StampEntryCancelledByCustomerAsync(Guid entryId, QueueEntryDetails? existing);

    // Operator-board writes. AssignEntryAsync takes a nullable operator id on purpose: null returns
    // the entry to the shared pool, which is a real destination, not an absence of one.
    Task AssignEntryAsync(Guid entryId, Guid? operatorId);
    Task MoveEntryToEndAsync(Guid entryId);
    Task ChangeEntryServiceAsync(Guid entryId, Guid serviceId);
    // The same progress_status set_queue_progress writes, but for an entry that is still waiting —
    // which that RPC refuses. See the note on the implementation.
    Task SetEntryNoteAsync(Guid entryId, string? note);
    // Today's completed visits, carrying serving_at/done_at so the caller can derive both the
    // count and the average service time from one read.
    Task<List<QueueEntryResponse>> GetCompletedTodayAsync(Guid businessId);
}
