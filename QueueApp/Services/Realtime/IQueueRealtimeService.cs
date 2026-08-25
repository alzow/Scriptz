namespace QueueApp.Services.Realtime;

public interface IQueueRealtimeService
{
    // filterColumn/filterValue scope the subscription to one Postgres Changes filter
    // (e.g. "business_id"/a business id for a single-business screen, or "customer_id"/the
    // signed-in user's id for a cross-business one like the Browse dashboard).
    Task SubscribeAsync(string filterColumn, string filterValue, Func<Task> onChange, string table = "queue_entries");
    Task UnsubscribeAsync();
}
