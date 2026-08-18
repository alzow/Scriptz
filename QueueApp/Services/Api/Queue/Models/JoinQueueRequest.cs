using System.Text.Json.Serialization;

namespace ScriptzApp.Services.Api.Queue.Models;

// Property names map to the join_queue SQL function's parameter names.
public class JoinQueueRequest
{
    [JsonPropertyName("p_business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("p_operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("p_service_id")] public Guid? ServiceId { get; set; }
    [JsonPropertyName("p_customer_id")] public Guid? CustomerId { get; set; }
    [JsonPropertyName("p_customer_name")] public string? CustomerName { get; set; }
    [JsonPropertyName("p_details")] public object? Details { get; set; }
}
