using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

public class SetBookingProgressRequest
{
    [JsonPropertyName("p_booking_id")] public Guid BookingId { get; set; }
    [JsonPropertyName("p_status")] public string? Status { get; set; }
}
