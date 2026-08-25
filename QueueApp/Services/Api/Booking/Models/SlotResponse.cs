using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

public class SlotResponse
{
    [JsonPropertyName("slot_start")] public DateTimeOffset SlotStart { get; set; }
    [JsonPropertyName("slot_end")] public DateTimeOffset SlotEnd { get; set; }

    // Only populated by get_available_slots_any's union-across-resources query — how many
    // resources are free at this slot. Null (and unused) on the single-operator path.
    [JsonPropertyName("free_count")] public int? FreeCount { get; set; }

    // Same fixed +2 SAST display conversion used throughout — SA has no DST.
    [JsonIgnore]
    public string TimeDisplay => SlotStart.ToOffset(TimeSpan.FromHours(2)).ToString("h:mm tt");
}
