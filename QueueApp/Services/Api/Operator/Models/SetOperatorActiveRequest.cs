using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

public class SetOperatorActiveRequest
{
    [JsonPropertyName("is_active")] public bool IsActive { get; set; }
}
