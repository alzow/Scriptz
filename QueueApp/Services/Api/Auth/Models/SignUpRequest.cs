using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Auth.Models;

public class SignUpRequest
{
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
    [JsonPropertyName("data")] public SignUpMetadata Data { get; set; } = new();
}

public class SignUpMetadata
{
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string Phone { get; set; } = string.Empty;
}
