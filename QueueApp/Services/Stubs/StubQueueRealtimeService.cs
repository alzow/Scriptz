using ScriptzApp.Services.Realtime;

namespace ScriptzApp.Services.Stubs;

// No-op: DEBUG builds work against StubQueueService's in-memory data, so there's
// no live backend to subscribe to.
public class StubQueueRealtimeService : IQueueRealtimeService
{
    public Task SubscribeAsync(Guid businessId, Func<Task> onChange) => Task.CompletedTask;

    public Task UnsubscribeAsync() => Task.CompletedTask;
}
