using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Business.Models;

public class BusinessResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "queue"; // "queue" | "booking"

    [JsonPropertyName("suburb")]
    public string Suburb { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    // Multi-resource businesses (e.g. a car wash with several bays) can let the system assign the
    // resource at the last responsible moment instead of making the customer pick one. Defaults
    // true so every existing single-operator business is unaffected.
    [JsonPropertyName("allow_operator_choice")]
    public bool AllowOperatorChoice { get; set; } = true;

    [JsonPropertyName("last_seen_at")]
    public DateTime? LastSeenAt { get; set; }

    [JsonIgnore]
    public bool IsAvailableNow =>
        IsActive && (Mode == "booking"
            || (LastSeenAt.HasValue && LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-15)));
}
