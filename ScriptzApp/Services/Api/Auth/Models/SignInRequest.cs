using System.Text.Json.Serialization;

namespace ScriptzApp.Services.Api.Auth.Models;

public class SignInRequest
{
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
}
