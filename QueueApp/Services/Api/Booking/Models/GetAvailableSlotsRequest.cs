using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

// p_lead_minutes/p_grid_minutes deliberately omitted so Postgres applies its own SQL-side
// defaults (30/15) rather than us sending an explicit null, which make_interval() would reject.
public class GetAvailableSlotsRequest
{
    [JsonPropertyName("p_operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("p_service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("p_date")] public string Date { get; set; } = "";
}
