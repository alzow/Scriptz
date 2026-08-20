using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

// Full-field update rather than a partial patch, to avoid null-vs-omitted ambiguity in the PATCH body.
public class UpdateOperatorRequest
{
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }
}
