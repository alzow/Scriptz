using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Auth.Models;

public class PhoneCheckRequest
{
    [JsonPropertyName("p_phone")] public string Phone { get; set; } = string.Empty;
}
