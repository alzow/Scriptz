using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Auth.Models;

public class RefreshTokenRequest
{
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
}
