using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Profile.Models;

public class UpdateProfileRequest
{
    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
}
