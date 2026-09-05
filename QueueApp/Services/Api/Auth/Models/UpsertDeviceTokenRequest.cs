using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Auth.Models;

public record UpsertDeviceTokenRequest(
    [property: JsonPropertyName("p_device_id")] string DeviceId,
    [property: JsonPropertyName("p_fcm_token")] string FcmToken,
    [property: JsonPropertyName("p_platform")] string Platform);