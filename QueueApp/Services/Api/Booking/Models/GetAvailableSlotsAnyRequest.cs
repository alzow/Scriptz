using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

// No p_operator_id — slots free on ANY resource at the business, unioned across all of them.
// Same p_lead_minutes/p_grid_minutes omission reasoning as GetAvailableSlotsRequest: leaving them
// out lets Postgres apply its own defaults rather than us sending an explicit null.
public class GetAvailableSlotsAnyRequest
{
    [JsonPropertyName("p_business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("p_service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("p_date")] public string Date { get; set; } = "";
}
