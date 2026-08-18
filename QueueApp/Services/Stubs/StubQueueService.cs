using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real QueueService in DEBUG builds.
public class StubQueueService : IQueueService
{
    private readonly List<QueueEntryResponse> _entries = new();

    public Task<List<QueueEntryResponse>> GetActiveEntriesAsync(Guid businessId)
        => Task.FromResult(_entries
            .Where(e => e.BusinessId == businessId && (e.Status == "waiting" || e.Status == "serving"))
            .OrderBy(e => e.JoinedAt)
            .ToList());

    public Task AddWalkInAsync(Guid businessId, Guid? operatorId, string name)
    {
        _entries.Add(new QueueEntryResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerName = name,
            Status = "waiting",
            JoinedAt = DateTime.UtcNow,
        });
        return Task.CompletedTask;
    }

    public Task StartServingAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null || entry.Status != "waiting")
            throw new InvalidOperationException("entry not in waiting state");

        var alreadyServing = _entries.Any(e =>
            e.BusinessId == entry.BusinessId && e.OperatorId == entry.OperatorId && e.Status == "serving");
        if (alreadyServing)
            throw new InvalidOperationException("this operator is already serving another customer");

        entry.Status = "serving";
        return Task.CompletedTask;
    }

    public Task CompleteAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.Status = "completed";
        return Task.CompletedTask;
    }

    public Task NoShowAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.Status = "no_show";
        return Task.CompletedTask;
    }

    public Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId)
    {
        var rows = _entries
            .Where(e => e.BusinessId == businessId && e.Status == "waiting")
            .GroupBy(e => e.OperatorId)
            .Select(g => new QueueSummaryRow
            {
                OperatorId = g.Key,
                OperatorName = g.Key.HasValue ? "Operator" : "Any available",
                WaitingCount = g.Count(),
                NewJoinWaitMinutes = g.Count() * 10,
            })
            .ToList();
        return Task.FromResult(rows);
    }

    public Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId)
    {
        var entry = new QueueEntryResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorId = operatorId,
            CustomerId = customerId,
            Status = "waiting",
            JoinedAt = DateTime.UtcNow,
        };
        _entries.Add(entry);
        return Task.FromResult(entry);
    }

    public Task<QueueEntryResponse> CancelEntryAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.Status = "cancelled";
        return Task.FromResult(entry ?? new QueueEntryResponse { Id = entryId, Status = "cancelled" });
    }

    public Task<MyQueueStatusResponse?> GetMyQueueStatusAsync(Guid businessId)
    {
        var waiting = _entries
            .Where(e => e.BusinessId == businessId && (e.Status == "waiting" || e.Status == "serving"))
            .OrderBy(e => e.JoinedAt)
            .ToList();

        var mine = waiting.LastOrDefault();
        if (mine is null)
            return Task.FromResult<MyQueueStatusResponse?>(null);

        var position = waiting
            .Where(e => e.OperatorId == mine.OperatorId)
            .OrderBy(e => e.JoinedAt)
            .ToList()
            .IndexOf(mine) + 1;

        return Task.FromResult<MyQueueStatusResponse?>(new MyQueueStatusResponse
        {
            EntryId = mine.Id,
            OperatorId = mine.OperatorId,
            OperatorName = mine.OperatorId.HasValue ? "Operator" : "Any available",
            Position = position,
            Status = mine.Status,
            JoinedAt = mine.JoinedAt,
        });
    }

    public Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId)
        => Task.FromResult<decimal?>(10);
}
