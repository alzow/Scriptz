using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

public class MyQueueStatusResponse
{
    [JsonPropertyName("entry_id")] public Guid EntryId { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("operator_name")] public string OperatorName { get; set; } = string.Empty;
    [JsonPropertyName("queue_position")] public int Position { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("joined_at")] public DateTime JoinedAt { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }

    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);
}
