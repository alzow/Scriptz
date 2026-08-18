using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

public class QueueSummaryRow
{
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("operator_name")] public string OperatorName { get; set; } = string.Empty;
    [JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
    [JsonPropertyName("waiting_count")] public int WaitingCount { get; set; }
    [JsonPropertyName("new_join_wait_minutes")] public double NewJoinWaitMinutes { get; set; }
}
