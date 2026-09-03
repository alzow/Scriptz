using System.Text.Json.Serialization;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Services.Api.Queue.Models;

// Property names map to the join_queue SQL function's parameter names.
public class JoinQueueRequest
{
    [JsonPropertyName("p_business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("p_operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("p_service_id")] public Guid? ServiceId { get; set; }
    [JsonPropertyName("p_customer_id")] public Guid? CustomerId { get; set; }
    [JsonPropertyName("p_customer_name")] public string? CustomerName { get; set; }
    [JsonPropertyName("p_details")] public object? Details { get; set; }

    // Omitted entirely unless the service asked something, so a join for a service with no intake
    // fields sends the same five parameters it always did — join_queue never sees this exist.
    // TODO: stub — join_queue needs a p_intake_responses jsonb parameter; see
    // Documentation/service-intake-fields-backend-requirements.md.
    [JsonPropertyName("p_intake_responses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, IntakeAnswer>? IntakeResponses { get; set; }
}
