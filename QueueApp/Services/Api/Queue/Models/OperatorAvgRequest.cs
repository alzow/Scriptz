using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

// Property name maps to the operator_avg_minutes SQL function's parameter name.
public class OperatorAvgRequest
{
    [JsonPropertyName("p_operator_id")] public Guid OperatorId { get; set; }
}
