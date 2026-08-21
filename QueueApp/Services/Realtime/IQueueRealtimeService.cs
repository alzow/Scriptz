namespace QueueApp.Services.Realtime;

public interface IQueueRealtimeService
{
    Task SubscribeAsync(Guid businessId, Func<Task> onChange, string table = "queue_entries");
    Task UnsubscribeAsync();
}
