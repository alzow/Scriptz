using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

public class CreateAvailabilityBlockRequest
{
    [JsonPropertyName("operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
}
