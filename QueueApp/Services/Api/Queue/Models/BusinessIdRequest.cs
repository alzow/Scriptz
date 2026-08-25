using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

public class BusinessIdRequest
{
    [JsonPropertyName("p_business_id")] public Guid BusinessId { get; set; }
}
