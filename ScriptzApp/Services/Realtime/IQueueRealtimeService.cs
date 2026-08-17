namespace ScriptzApp.Services.Realtime;

public interface IQueueRealtimeService
{
    Task SubscribeAsync(Guid businessId, Func<Task> onChange);
    Task UnsubscribeAsync();
}
