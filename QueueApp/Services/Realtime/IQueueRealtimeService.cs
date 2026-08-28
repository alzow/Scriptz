namespace QueueApp.Services.Realtime;

public interface IQueueRealtimeService
{
    // owner scopes the subscription to one caller (a view model, normally `this`) so a screen
    // tearing its feed down can never take another screen's feed with it — tab switches raise the
    // incoming page's Appearing and the outgoing page's Disappearing in either order.
    //
    // filterColumn/filterValue scope the feed to one Postgres Changes filter (e.g. "business_id"
    // for a single-business screen, or "customer_id" for a cross-business one like Browse).
    Task SubscribeAsync(object owner, string filterColumn, string filterValue, Func<Task> onChange, string table = "queue_entries");

    Task UnsubscribeAsync(object owner);
}
