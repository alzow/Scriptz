using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.ServiceOfferings.Models;

public class SetServiceActiveRequest
{
    [JsonPropertyName("is_active")] public bool IsActive { get; set; }
}
