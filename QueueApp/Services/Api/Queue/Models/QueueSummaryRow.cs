using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Services.Api.Queue.Models;

public partial class QueueSummaryRow : ObservableObject
{
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("operator_name")] public string OperatorName { get; set; } = string.Empty;
    [JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
    [JsonPropertyName("waiting_count")] public int WaitingCount { get; set; }
    [JsonPropertyName("serving_count")] public int ServingCount { get; set; }
    [JsonPropertyName("new_join_wait_minutes")] public double NewJoinWaitMinutes { get; set; }

    // Per-row loading state so tapping Join on one operator doesn't spin every button.
    [JsonIgnore] [ObservableProperty] private bool _isJoining;
}
