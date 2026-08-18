using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Business.Models;

public class BusinessResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
