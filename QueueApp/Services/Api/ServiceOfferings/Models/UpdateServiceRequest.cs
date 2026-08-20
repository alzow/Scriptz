using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.ServiceOfferings.Models;

// Full-field update rather than a partial patch, to avoid null-vs-omitted ambiguity in the PATCH body.
public class UpdateServiceRequest
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("price_cents")] public int? PriceCents { get; set; }
    [JsonPropertyName("est_minutes")] public int EstMinutes { get; set; }
}
