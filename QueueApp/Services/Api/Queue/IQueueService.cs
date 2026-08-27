using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Queue;

public interface IQueueService
{
    Task<List<QueueEntryResponse>> GetActiveEntriesAsync(Guid businessId);
    Task AddWalkInAsync(Guid businessId, Guid? operatorId, string? name, Guid serviceId);
    Task StartServingAsync(Guid entryId);
    Task CompleteAsync(Guid entryId);
    Task NoShowAsync(Guid entryId);
    Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId);
    Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId, string? customerName, Guid serviceId);
    Task<QueueEntryResponse> CancelEntryAsync(Guid entryId);
    Task<MyQueueStatusResponse?> GetMyQueueStatusAsync(Guid businessId);
    Task<MyActiveQueueEntryResponse?> GetMyActiveEntryAsync();
    Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId);
    Task<QueueEntryResponse> SetQueueProgressAsync(Guid entryId, string? status);
    Task<List<VisitResponse>> GetMyVisitsAsync(Guid customerId);

    // Operator-board writes. AssignEntryAsync takes a nullable operator id on purpose: null returns
    // the entry to the shared pool, which is a real destination, not an absence of one.
    Task AssignEntryAsync(Guid entryId, Guid? operatorId);
    Task MoveEntryToEndAsync(Guid entryId);
    Task ChangeEntryServiceAsync(Guid entryId, Guid serviceId);
    // Today's completed visits, carrying serving_at/done_at so the caller can derive both the
    // count and the average service time from one read.
    Task<List<QueueEntryResponse>> GetCompletedTodayAsync(Guid businessId);
}
