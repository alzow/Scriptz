using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

// Cross-business version of MyQueueStatusResponse — the browse dashboard doesn't know which
// business the customer is queued at, so it can't call my_queue_status(business_id). Backed by
// a new my_active_queue_entry() RPC; see Documentation/README-UI-ENHANCEMENTS-SUPABASE.md.
public class MyActiveQueueEntryResponse
{
    [JsonPropertyName("entry_id")] public Guid EntryId { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("business_name")] public string BusinessName { get; set; } = string.Empty;
    [JsonPropertyName("business_latitude")] public double? BusinessLatitude { get; set; }
    [JsonPropertyName("business_longitude")] public double? BusinessLongitude { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("operator_name")] public string OperatorName { get; set; } = string.Empty;
    [JsonPropertyName("queue_position")] public int Position { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("joined_at")] public DateTime JoinedAt { get; set; }
    [JsonPropertyName("wait_minutes")] public decimal? WaitMinutes { get; set; }

    [JsonIgnore] public bool IsBeingServed => Status == "serving";
}
