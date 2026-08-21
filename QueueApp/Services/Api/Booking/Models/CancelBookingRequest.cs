using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

public class CancelBookingRequest
{
    [JsonPropertyName("p_booking_id")] public Guid BookingId { get; set; }
}
