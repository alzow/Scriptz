namespace QueueApp.Services.Api.Queue.Models;

// The queue_status enum's labels were never captured by the schema verification (see
// Documentation/SUPABASE-SCHEMA-VERIFIED.md), so everything except the two the app itself sends is
// matched loosely rather than compared to a label this can only guess at.
public static class QueueEntryStatuses
{
    public const string Waiting = "waiting";
    public const string Serving = "serving";

    // TODO: stub pending Documentation/awaiting-collection-backend-requirements.md — swap this
    // constant for the real enum label once that spec lands.
    public const string AwaitingCollection = "awaiting_collection";

    public static bool IsCancelled(string? status) =>
        status is not null && status.Contains("cancel", StringComparison.OrdinalIgnoreCase);

    public static bool IsNoShow(string? status) =>
        status is not null &&
        (status.Contains("no_show", StringComparison.OrdinalIgnoreCase) ||
         status.Contains("noshow", StringComparison.OrdinalIgnoreCase));
}
