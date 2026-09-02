using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

// Cross-business version of MyQueueStatusResponse — the browse dashboard doesn't know which
// business the customer is queued at, so it can't call my_queue_status(business_id). Backed by
// a new my_active_queue_entry() RPC; see Documentation/README-SUPABASE-SETUP.md.
public class MyActiveQueueEntryResponse
{
    [JsonPropertyName("entry_id")] public Guid EntryId { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("business_name")] public string BusinessName { get; set; } = string.Empty;
    [JsonPropertyName("business_latitude")] public double? BusinessLatitude { get; set; }
    [JsonPropertyName("business_longitude")] public double? BusinessLongitude { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("operator_name")] public string? OperatorName { get; set; }
    [JsonPropertyName("queue_position")] public int Position { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("joined_at")] public DateTime JoinedAt { get; set; }
    [JsonPropertyName("wait_minutes")] public decimal? WaitMinutes { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }

    [JsonIgnore] public bool IsBeingServed => Status == "serving";
    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);

    // join_queue picks an operator now, so an entry with none is the one case it could not: a shop
    // with nobody on shift. Nothing about that entry can be stated in an operator's terms — it has
    // no place in anyone's line, and its wait is whatever the first person to come free makes it,
    // which compute_wait_minutes has no way to express. So the screens say less, rather than
    // saying "1st, 0 min" and being wrong twice.
    [JsonIgnore] public bool HasOperator => OperatorId is not null;
    [JsonIgnore] public bool IsUnassigned => OperatorId is null;
    [JsonIgnore] public bool HasWaitEstimate => HasOperator && WaitMinutes.HasValue;
    [JsonIgnore] public bool ShowWaitEstimate => HasWaitEstimate && !IsBeingServed;
    [JsonIgnore] public bool ShowUnassignedNotice => IsUnassigned && !IsBeingServed;
    [JsonIgnore] public bool ShowServedByName => IsBeingServed && HasOperator;
    [JsonIgnore] public bool ShowServedAnonymously => IsBeingServed && IsUnassigned;
}
