using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.ServiceOfferings.Models;

public class ServiceResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("price_cents")] public int? PriceCents { get; set; }
    [JsonPropertyName("est_minutes")] public int EstMinutes { get; set; }
    [JsonPropertyName("is_active")] public bool IsActive { get; set; }
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }

    // TODO: stub pending Documentation/awaiting-collection-backend-requirements.md — swap the
    // column name for the real one once that spec lands.
    [JsonPropertyName("requires_collection")] public bool RequiresCollection { get; set; }

    [JsonIgnore] public bool IsToggling { get; set; }

    [JsonIgnore]
    public string PriceDisplay => PriceCents.HasValue ? $"R{PriceCents.Value / 100m:0.##}" : "No price set";
}
