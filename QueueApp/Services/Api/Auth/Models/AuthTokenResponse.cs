using System.Text.Json.Serialization;

namespace ScriptzApp.Services.Api.Auth.Models;

public class AuthTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("token_type")] public string TokenType { get; set; } = string.Empty;
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("user")] public AuthUser? User { get; set; }
}

public class AuthUser
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
}
