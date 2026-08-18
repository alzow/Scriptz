using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real QueueService in DEBUG builds.
public class StubQueueService : IQueueService
{
    private readonly List<QueueEntryResponse> _entries = new();

    public Task<List<QueueEntryResponse>> GetWaitingAsync(Guid businessId)
        => Task.FromResult(_entries.Where(e => e.Status == "waiting").OrderBy(e => e.JoinedAt).ToList());

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
        if (entry != null) entry.Status = "serving";
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
}
