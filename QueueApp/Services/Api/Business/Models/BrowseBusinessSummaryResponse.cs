using System.Text.Json.Serialization;
using QueueApp.Features.CategoryPicker;

namespace QueueApp.Services.Api.Business.Models;

// Powers the Browse dashboard's "Open now near you" list — one row per business with a live
// wait aggregate already attached, so the screen doesn't fire one queue-summary call per card.
// Backed by a new nearby_business_summary(category, suburb) RPC; see
// Documentation/README-UI-ENHANCEMENTS-SUPABASE.md.
public class BrowseBusinessSummaryResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
    [JsonPropertyName("mode")] public string Mode { get; set; } = "queue"; // "queue" | "booking"
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("latitude")] public double? Latitude { get; set; }
    [JsonPropertyName("longitude")] public double? Longitude { get; set; }
    [JsonPropertyName("distance_km")] public double? DistanceKm { get; set; }
    [JsonPropertyName("is_active")] public bool IsActive { get; set; }
    [JsonPropertyName("last_seen_at")] public DateTime? LastSeenAt { get; set; }
    [JsonPropertyName("waiting_count")] public int WaitingCount { get; set; }
    [JsonPropertyName("operators_working_count")] public int OperatorsWorkingCount { get; set; }
    [JsonPropertyName("avg_wait_minutes")] public decimal? AvgWaitMinutes { get; set; }
    [JsonPropertyName("next_slot_starts_at")] public DateTimeOffset? NextSlotStartsAt { get; set; }

    [JsonIgnore]
    public bool IsAvailableNow =>
        IsActive && (Mode == "booking"
            || (LastSeenAt.HasValue && LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-15)));

    // "go" ≤10 min, "wait" 11-30 min, "busy" 30+ min, "book" = booking-mode business, "off" = closed.
    [JsonIgnore]
    public string WaitBucket
    {
        get
        {
            if (!IsAvailableNow) return "off";
            if (Mode == "booking") return "book";
            if (!AvgWaitMinutes.HasValue) return "unknown";
            return AvgWaitMinutes.Value switch
            {
                <= 10 => "go",
                <= 30 => "wait",
                _ => "busy",
            };
        }
    }

    [JsonIgnore]
    public string PillText => WaitBucket switch
    {
        "off" => "Closed",
        "book" => "Booking",
        "unknown" => "—",
        _ => $"{AvgWaitMinutes:0} min",
    };

    [JsonIgnore]
    public string SubText
    {
        get
        {
            if (WaitBucket == "off") return "Currently closed";
            if (WaitBucket == "book")
                return NextSlotStartsAt.HasValue
                    ? $"Next slot {NextSlotStartsAt.Value.ToOffset(TimeSpan.FromHours(2)):ddd HH:mm}"
                    : "By appointment";

            var staffNoun = OperatorsWorkingCount == 1 ? "staff" : "staff";
            return $"{WaitingCount} ahead · {OperatorsWorkingCount} {staffNoun} working";
        }
    }

    // 0-1 fill for the wait bar; 45 min treated as "full".
    [JsonIgnore]
    public double WaitFraction => Math.Clamp((double)(AvgWaitMinutes ?? 0) / 45.0, 0, 1);

    [JsonIgnore]
    public string CategoryIcon => CategoryCatalog.All.FirstOrDefault(c => c.Key == Category)?.Icon ?? "🏪";

    [JsonIgnore]
    public string CategoryDisplay => CategoryCatalog.All.FirstOrDefault(c => c.Key == Category)?.Display ?? Category;

    // Distance is only known once the customer's location has resolved (see ILocationService) —
    // degrades to just the category label rather than showing a blank/broken "· km" fragment.
    [JsonIgnore]
    public string MetaText => DistanceKm.HasValue
        ? $"{CategoryDisplay} · {DistanceKm:0.0} km"
        : CategoryDisplay;
}
