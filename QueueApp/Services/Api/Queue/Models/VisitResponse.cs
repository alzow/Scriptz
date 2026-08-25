using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

public class VisitResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("visited_at")] public DateTime VisitedAt { get; set; }
    [JsonPropertyName("business")] public VisitBusinessRef? Business { get; set; }
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }

    [JsonIgnore] public Guid BusinessId => Business?.Id ?? Guid.Empty;
    [JsonIgnore] public string BusinessName => Business?.Name ?? "Unknown business";
    [JsonIgnore] public string OperatorName => Operator?.DisplayName ?? "Any available";
    [JsonIgnore] public string ServiceLabel => Service?.Name ?? "";
}

public class VisitBusinessRef
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public class VisitOperatorRef
{
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
}

public class VisitServiceRef
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}
