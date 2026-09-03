namespace QueueApp.Services.Api.Queue.Models;

// The queue_status enum's labels were never captured by the schema verification (see
// Documentation/SUPABASE-SCHEMA-VERIFIED.md), so everything except the labels the app itself sends
// is matched loosely rather than compared to a label this can only guess at.
public static class QueueEntryStatuses
{
    public const string Waiting = "waiting";
    public const string Serving = "serving";
    public const string Done = "done";

    public const string AwaitingCollection = "awaiting_collection";

    public static bool IsCancelled(string? status) =>
        status is not null && status.Contains("cancel", StringComparison.OrdinalIgnoreCase);

    public static bool IsNoShow(string? status) =>
        status is not null &&
        (status.Contains("no_show", StringComparison.OrdinalIgnoreCase) ||
         status.Contains("noshow", StringComparison.OrdinalIgnoreCase));
}
