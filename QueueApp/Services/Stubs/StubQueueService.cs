using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real QueueService in DEBUG builds.
public class StubQueueService : IQueueService
{
    private readonly Guid _defaultBusinessId = new("0637f5ef-c7fa-46dc-b4e5-b814f2d7d3bf");

    private readonly List<OperatorResponse> _operators = new()
    {
        new() { Id = Guid.NewGuid(), DisplayName = "Chair 1", SortOrder = 0, IsAvailable = true },
        new() { Id = Guid.NewGuid(), DisplayName = "Chair 2", SortOrder = 1, IsAvailable = true },
    };

    private readonly List<QueueEntryResponse> _entries = new();

    public Task<Guid> GetOwnedBusinessIdAsync() => Task.FromResult(_defaultBusinessId);

    public Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId)
        => Task.FromResult(_operators.OrderBy(o => o.SortOrder).ToList());

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

    public Task HeartbeatAsync(Guid businessId) => Task.CompletedTask;
}
