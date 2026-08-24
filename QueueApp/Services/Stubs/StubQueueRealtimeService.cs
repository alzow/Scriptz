using QueueApp.Services.Realtime;

namespace QueueApp.Services.Stubs;

// No-op: DEBUG builds work against StubQueueService's in-memory data, so there's
// no live backend to subscribe to.
public class StubQueueRealtimeService : IQueueRealtimeService
{
    public Task SubscribeAsync(string filterColumn, string filterValue, Func<Task> onChange, string table = "queue_entries") => Task.CompletedTask;

    public Task UnsubscribeAsync() => Task.CompletedTask;
}
