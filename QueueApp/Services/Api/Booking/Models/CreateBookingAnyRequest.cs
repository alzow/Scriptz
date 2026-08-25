using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

// No p_operator_id — the customer didn't (couldn't) choose one; create_booking_any assigns
// whichever resource is actually free at that slot.
public class CreateBookingAnyRequest
{
    [JsonPropertyName("p_business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("p_service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("p_customer_id")] public Guid CustomerId { get; set; }
    [JsonPropertyName("p_starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("p_note")] public string? Note { get; set; }
}
