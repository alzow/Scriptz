using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

public class SlotResponse
{
    [JsonPropertyName("slot_start")] public DateTimeOffset SlotStart { get; set; }
    [JsonPropertyName("slot_end")] public DateTimeOffset SlotEnd { get; set; }

    // Same fixed +2 SAST display conversion used throughout — SA has no DST.
    [JsonIgnore]
    public string TimeDisplay => SlotStart.ToOffset(TimeSpan.FromHours(2)).ToString("h:mm tt");
}
