using QueueApp.Framework.Base;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class QueueService : BaseService, IQueueService
{
    private readonly IQueueApi _api;

    public QueueService(IQueueApi api)
    {
        _api = api;
    }

    public Task<List<QueueEntryResponse>> GetActiveEntriesAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetActiveEntriesAsync($"eq.{businessId}"));

    public Task AddWalkInAsync(Guid businessId, Guid? operatorId, string? name, Guid serviceId) =>
        ExecuteApiCallAsync(_api.JoinQueueAsync(new JoinQueueRequest
        {
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerName = name,
            ServiceId = serviceId,
        }));

    public Task StartServingAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.StartServingAsync(new EntryIdRequest { EntryId = entryId }));

    public Task CompleteAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.CompleteEntryAsync(new EntryIdRequest { EntryId = entryId }));

    public Task NoShowAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.MarkNoShowAsync(new EntryIdRequest { EntryId = entryId }));

    public Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetQueueSummaryAsync(new BusinessIdRequest { BusinessId = businessId }));

    public Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId, string? customerName, Guid serviceId) =>
        ExecuteApiCallAsync(_api.JoinQueueAsync(new JoinQueueRequest
        {
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerId = customerId,
            CustomerName = customerName,
            ServiceId = serviceId,
        }));

    public Task<QueueEntryResponse> CancelEntryAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.CancelEntryAsync(new EntryIdRequest { EntryId = entryId }));

    public async Task<MyQueueStatusResponse?> GetMyQueueStatusAsync(Guid businessId)
    {
        var results = await ExecuteApiCallAsync(_api.GetMyQueueStatusAsync(new BusinessIdRequest { BusinessId = businessId }));
        return results.FirstOrDefault();
    }

    public async Task<MyActiveQueueEntryResponse?> GetMyActiveEntryAsync()
    {
        var results = await ExecuteApiCallAsync(_api.GetMyActiveEntryAsync());
        return results.FirstOrDefault();
    }

    public Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.GetEntryWaitMinutesAsync(new QueueEntryIdRequest { EntryId = entryId }));

    public Task<QueueEntryResponse> SetQueueProgressAsync(Guid entryId, string? status) =>
        ExecuteApiCallAsync(_api.SetQueueProgressAsync(new SetProgressRequest { EntryId = entryId, Status = status }));

    public Task<List<MyQueueEntryResponse>> GetMyEntriesAsync(Guid customerId) =>
        ExecuteApiCallAsync(_api.GetMyEntriesAsync($"eq.{customerId}"));

    public async Task<MyQueueEntryResponse?> GetEntryAsync(Guid entryId)
    {
        var rows = await ExecuteApiCallAsync(_api.GetEntryAsync($"eq.{entryId}"));
        return rows.FirstOrDefault();
    }

    // The "owner or self manage" update policy already covers the customer writing to their own
    // row, so attributing a cancellation needs no new SQL.
    public Task StampEntryCancelledByCustomerAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync($"eq.{entryId}",
            new Dictionary<string, object?> { ["details"] = QueueEntryDetails.CancelledByCustomer() }));

    public Task AssignEntryAsync(Guid entryId, Guid? operatorId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync($"eq.{entryId}",
            new Dictionary<string, object?> { ["operator_id"] = operatorId }));

    // Position within a queue is joined_at order, so "move to end" is a re-stamp rather than a
    // separate ordering column. UTC because joined_at is timestamptz.
    public Task MoveEntryToEndAsync(Guid entryId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync($"eq.{entryId}",
            new Dictionary<string, object?> { ["joined_at"] = DateTime.UtcNow }));

    public Task ChangeEntryServiceAsync(Guid entryId, Guid serviceId) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync($"eq.{entryId}",
            new Dictionary<string, object?> { ["service_id"] = serviceId }));

    // set_queue_progress raises "entry not currently being served" on anything but a serving row,
    // so a note for someone still in the line goes through the same owner-update policy the other
    // board writes use. Same column, so the customer reads it as the one "latest update" either way.
    public Task SetEntryNoteAsync(Guid entryId, string? note) =>
        ExecuteApiCallAsync(_api.UpdateEntryAsync($"eq.{entryId}",
            new Dictionary<string, object?> { ["progress_status"] = note }));

    // "Today" is the device's local day boundary — the shop reads these tiles standing in its own
    // timezone, not UTC.
    public Task<List<QueueEntryResponse>> GetCompletedTodayAsync(Guid businessId)
    {
        var since = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local).ToUniversalTime();
        return ExecuteApiCallAsync(_api.GetCompletedSinceAsync(
            $"eq.{businessId}", $"gte.{since:yyyy-MM-ddTHH:mm:ssZ}"));
    }
}
