using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

public class MyQueueStatusResponse
{
    [JsonPropertyName("entry_id")] public Guid EntryId { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    // Null once nobody has been assigned — my_queue_status used to coalesce this to the literal
    // "Any available", which put the wording in the database where only one screen's phrasing fits.
    [JsonPropertyName("operator_name")] public string? OperatorName { get; set; }
    [JsonPropertyName("queue_position")] public int Position { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("joined_at")] public DateTime JoinedAt { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }

    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);
}
