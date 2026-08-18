using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

public class OperatorResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }
}
