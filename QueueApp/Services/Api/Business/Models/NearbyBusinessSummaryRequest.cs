using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Business.Models;

public class NearbyBusinessSummaryRequest
{
    [JsonPropertyName("p_category")] public string? Category { get; set; }
    [JsonPropertyName("p_suburb")] public string Suburb { get; set; } = "Lenasia";
}
