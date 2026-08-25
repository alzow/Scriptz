using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

public class CreateBookingRequest
{
    [JsonPropertyName("p_business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("p_operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("p_service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("p_customer_id")] public Guid CustomerId { get; set; }
    [JsonPropertyName("p_starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("p_note")] public string? Note { get; set; }
}
