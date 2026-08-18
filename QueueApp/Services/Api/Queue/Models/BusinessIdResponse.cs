using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

public class BusinessIdResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
}
