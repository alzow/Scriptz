using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

public class CreateOperatorRequest
{
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }
}
