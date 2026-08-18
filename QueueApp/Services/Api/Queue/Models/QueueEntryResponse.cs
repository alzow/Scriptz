using System.Text.Json.Serialization;

namespace ScriptzApp.Services.Api.Queue.Models;

public class QueueEntryResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("service_id")] public Guid? ServiceId { get; set; }
    [JsonPropertyName("customer_id")] public Guid? CustomerId { get; set; }
    [JsonPropertyName("customer_name")] public string? CustomerName { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "waiting";
    [JsonPropertyName("joined_at")] public DateTime JoinedAt { get; set; }
}
