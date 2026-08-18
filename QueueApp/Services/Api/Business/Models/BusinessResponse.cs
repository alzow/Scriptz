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

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("last_seen_at")]
    public DateTime? LastSeenAt { get; set; }

    [JsonIgnore]
    public bool IsAvailableNow =>
        IsActive && (Mode == "booking"
            || (LastSeenAt.HasValue && LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-15)));
}
