using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

// operators.is_available is the shift flag ("on shift" / "off shift"), distinct from is_active,
// which is whether the operator exists on the roster at all.
public class SetOperatorAvailableRequest
{
    [JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
}
