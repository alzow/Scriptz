using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

public class AvailabilityBlockResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }

    [JsonIgnore] public bool IsDeleting { get; set; }

    // SA has no DST, so a fixed +2 conversion for display is safe — same assumption the slot engine makes.
    [JsonIgnore] private DateTimeOffset LocalStart => StartsAt.ToOffset(TimeSpan.FromHours(2));
    [JsonIgnore] private DateTimeOffset LocalEnd => EndsAt.ToOffset(TimeSpan.FromHours(2));

    [JsonIgnore]
    public bool IsFullDay => LocalStart.TimeOfDay == TimeSpan.Zero && (LocalEnd - LocalStart).TotalHours >= 24;

    [JsonIgnore]
    public string RangeDisplay => IsFullDay
        ? LocalStart.ToString("ddd d MMM")
        : $"{LocalStart:ddd d MMM} · {LocalStart:h:mm tt}–{LocalEnd:h:mm tt}";

    [JsonIgnore]
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);
}
