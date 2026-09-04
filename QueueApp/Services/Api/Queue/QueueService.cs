using QueueApp.Framework.Base;
using QueueApp.Services.Api;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

public class QueueService : BaseService, IQueueService
{
    private readonly IQueueApi _api;

    public QueueService(IQueueApi api)
    {
        _api = api;
    }

    public Task<List<QueueEntryResponse>> GetActiveEntriesAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetActiveEntriesAsync(PostgrestFilter.Eq(businessId)));

    public Task<QueueEntryResponse> JoinQueueAsOperatorAsync(Guid businessId, Guid? operatorId, string? customerName, Guid serviceId,
        Dictionary<string, IntakeAnswer>? intakeResponses = null) =>
        ExecuteApiCallAsync(_api.JoinQueueAsync(new JoinQueueRequest
        {
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerName = customerName,
            ServiceId = serviceId,
            IntakeResponses = intakeResponses,
        }));

    public Task StartServingAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.StartServingAsync(new EntryIdRequest { EntryId = entryId }));

    public Task CompleteAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.CompleteEntryAsync(new EntryIdRequest { EntryId = entryId }));

    public Task MarkAwaitingCollectionAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync(PostgrestFilter.Eq(entryId),
            new Dictionary<string, object?>
            {
                ["status"] = QueueEntryStatuses.AwaitingCollection,
                ["awaiting_collection_at"] = DateTime.UtcNow,
            }));

    // PATCHes status directly rather than going through complete_entry: that RPC's own state
    // machine requires status='serving' and rejects anything else with "entry is not completable",
    // but by the time this runs the entry is already in awaiting_collection, not serving.
    public Task MarkCollectedAsync(Guid entryId)
    {
        var now = DateTime.UtcNow;
        return ExecuteApiCallAsync(_api.UpdateEntryAsync(PostgrestFilter.Eq(entryId),
            new Dictionary<string, object?>
            {
                ["status"] = QueueEntryStatuses.Done,
                ["done_at"] = now,
                ["collected_at"] = now,
            }));
    }

    public Task NoShowAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.MarkNoShowAsync(new EntryIdRequest { EntryId = entryId }));

    public Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetQueueSummaryAsync(new BusinessIdRequest { BusinessId = businessId }));

    public Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId, string? customerName, Guid serviceId,
        Dictionary<string, IntakeAnswer>? intakeResponses = null) =>
        ExecuteApiCallAsync(_api.JoinQueueAsync(new JoinQueueRequest
        {
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerId = customerId,
            CustomerName = customerName,
            ServiceId = serviceId,
            IntakeResponses = intakeResponses,
        }));

    public Task<QueueEntryResponse> CancelEntryAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.CancelEntryAsync(new EntryIdRequest { EntryId = entryId }));

    public Task<MyQueueStatusResponse?> GetMyQueueStatusAsync(Guid businessId) =>
        ExecuteSingleAsync(_api.GetMyQueueStatusAsync(new BusinessIdRequest { BusinessId = businessId }));

    public Task<MyActiveQueueEntryResponse?> GetMyActiveEntryAsync() =>
        ExecuteSingleAsync(_api.GetMyActiveEntryAsync());

    public Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.GetEntryWaitMinutesAsync(new QueueEntryIdRequest { EntryId = entryId }));

    public Task<QueueEntryResponse> SetQueueProgressAsync(Guid entryId, string? status) =>
        ExecuteApiCallAsync(_api.SetQueueProgressAsync(new SetProgressRequest { EntryId = entryId, Status = status }));

    public Task<List<MyQueueEntryResponse>> GetMyEntriesAsync(Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyEntriesAsync(PostgrestFilter.Eq(customerId)));

    public Task<MyQueueEntryResponse?> GetEntryAsync(Guid entryId) =>
        ExecuteSingleAsync(_api.GetEntryAsync(PostgrestFilter.Eq(entryId)));

    // The "owner or self manage" update policy already covers the customer writing to their own
    // row, so attributing a cancellation needs no new SQL.
    //
    // The details the entry already has come in from the caller, because this PATCH replaces the
    // whole column and join_queue now writes an assignment stamp into it. Dropping that stamp on
    // the way out of the queue would be silent, and only visible much later as a row the board
    // thinks somebody hand-picked.
    public Task StampEntryCancelledByCustomerAsync(Guid entryId, QueueEntryDetails? existing) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync(PostgrestFilter.Eq(entryId),
            new Dictionary<string, object?> { ["details"] = QueueEntryDetails.CancelledByCustomer(existing) }));

    public Task AssignEntryAsync(Guid entryId, Guid? operatorId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync(PostgrestFilter.Eq(entryId),
            new Dictionary<string, object?> { ["operator_id"] = operatorId }));

    // Position within a queue is joined_at order, so "move to end" is a re-stamp rather than a
    // separate ordering column. UTC because joined_at is timestamptz.
    public Task MoveEntryToEndAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync(PostgrestFilter.Eq(entryId),
            new Dictionary<string, object?> { ["joined_at"] = DateTime.UtcNow }));

    public Task ChangeEntryServiceAsync(Guid entryId, Guid serviceId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync(PostgrestFilter.Eq(entryId),
            new Dictionary<string, object?> { ["service_id"] = serviceId }));

    // set_queue_progress raises "entry not currently being served" on anything but a serving row,
    // so a note for someone still in the line goes through the same owner-update policy the other
    // board writes use. Same column, so the customer reads it as the one "latest update" either way.
    public Task SetEntryNoteAsync(Guid entryId, string? note) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync(PostgrestFilter.Eq(entryId),
            new Dictionary<string, object?> { ["progress_status"] = note }));

    // "Today" is the device's local day boundary — the shop reads these tiles standing in its own
    // timezone, not UTC.
    public Task<List<QueueEntryResponse>> GetCompletedTodayAsync(Guid businessId)
    {
        var since = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local).ToUniversalTime();
        return ExecuteApiCallAsync(_api.GetCompletedSinceAsync(
            PostgrestFilter.Eq(businessId), PostgrestFilter.GteUtc(since)));
    }
}
