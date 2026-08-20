using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.ServiceOfferings.Models;

public class CreateServiceRequest
{
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("price_cents")] public int? PriceCents { get; set; }
    [JsonPropertyName("est_minutes")] public int EstMinutes { get; set; }
}
