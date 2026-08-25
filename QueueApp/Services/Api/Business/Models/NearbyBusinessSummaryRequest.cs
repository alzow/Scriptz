using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Business.Models;

public class NearbyBusinessSummaryRequest
{
    [JsonPropertyName("p_category")] public string? Category { get; set; }
    [JsonPropertyName("p_suburb")] public string Suburb { get; set; } = "Lenasia";

    // Customer's current device location, when known — lets nearby_business_summary compute
    // distance_km and order nearest-first. Never persisted server-side; sent fresh per request.
    [JsonPropertyName("p_customer_lat")] public double? CustomerLatitude { get; set; }
    [JsonPropertyName("p_customer_lng")] public double? CustomerLongitude { get; set; }
}
