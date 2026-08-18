using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Profile.Models;

public class ProfileResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
}
