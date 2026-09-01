using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

// The embedded shapes shared by every "what did I do here" read — queue entries and bookings both
// project the same business/operator/service references.
public class VisitBusinessRef
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "other";
}

public class VisitOperatorRef
{
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
}

public class VisitServiceRef
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("price_cents")] public int? PriceCents { get; set; }
}
