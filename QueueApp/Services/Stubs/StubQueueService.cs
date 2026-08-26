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

    public Task AddWalkInAsync(Guid businessId, Guid? operatorId, string? name, Guid serviceId)
    {
        _entries.Add(new QueueEntryResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorId = operatorId,
            ServiceId = serviceId,
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
        entry.ServingAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task CompleteAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
        {
            entry.Status = "completed";
            entry.DoneAt = DateTime.UtcNow;
        }
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

    public Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId, string? customerName, Guid serviceId)
    {
        var entry = new QueueEntryResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorId = operatorId,
            ServiceId = serviceId,
            CustomerId = customerId,
            CustomerName = customerName,
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

    public Task<MyActiveQueueEntryResponse?> GetMyActiveEntryAsync()
    {
        var mine = _entries
            .Where(e => e.Status is "waiting" or "serving")
            .OrderBy(e => e.JoinedAt)
            .LastOrDefault();

        if (mine is null)
            return Task.FromResult<MyActiveQueueEntryResponse?>(null);

        var position = _entries
            .Where(e => e.BusinessId == mine.BusinessId && e.OperatorId == mine.OperatorId
                        && e.Status is "waiting" or "serving")
            .OrderBy(e => e.JoinedAt)
            .ToList()
            .IndexOf(mine) + 1;

        return Task.FromResult<MyActiveQueueEntryResponse?>(new MyActiveQueueEntryResponse
        {
            EntryId = mine.Id,
            BusinessId = mine.BusinessId,
            BusinessName = "Nu-Look Barbers",
            BusinessLatitude = -26.3167,
            BusinessLongitude = 27.8500,
            OperatorId = mine.OperatorId,
            OperatorName = mine.OperatorId.HasValue ? "Operator" : "Any available",
            Position = position,
            Status = mine.Status,
            JoinedAt = mine.JoinedAt,
            WaitMinutes = position * 7,
            ProgressStatus = mine.ProgressStatus,
        });
    }

    public Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId)
        => Task.FromResult<decimal?>(10);

    public Task<QueueEntryResponse> SetQueueProgressAsync(Guid entryId, string? status)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.ProgressStatus = status;
        return Task.FromResult(entry ?? new QueueEntryResponse { Id = entryId, ProgressStatus = status });
    }

    public Task<List<VisitResponse>> GetMyVisitsAsync(Guid customerId)
        => Task.FromResult(new List<VisitResponse>());

    public Task AssignEntryAsync(Guid entryId, Guid? operatorId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.OperatorId = operatorId;
        return Task.CompletedTask;
    }

    public Task MoveEntryToEndAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.JoinedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task ChangeEntryServiceAsync(Guid entryId, Guid serviceId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.ServiceId = serviceId;
        return Task.CompletedTask;
    }

    public Task<int> GetCompletedTodayCountAsync(Guid businessId)
        => Task.FromResult(_entries.Count(e =>
            e.BusinessId == businessId && e.Status is "done" or "completed"
            && e.DoneAt >= DateTime.UtcNow.Date));

    // Null on purpose: exercises the em-dash path the real function takes until an operator has
    // three completed services on the books.
    public Task<decimal?> GetOperatorAvgMinutesAsync(Guid operatorId)
        => Task.FromResult<decimal?>(null);
}
