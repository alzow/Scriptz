using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Auth.Models;

public record RemoveDeviceTokenRequest(
    [property: JsonPropertyName("p_device_id")] string DeviceId);