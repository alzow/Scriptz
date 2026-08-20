using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

public class OperatorAvailabilityResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("day_of_week")] public int DayOfWeek { get; set; } // 0=Sun..6=Sat

    [JsonPropertyName("start_time")]
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan StartTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan EndTime { get; set; }

    [JsonIgnore] public bool IsDeleting { get; set; }

    [JsonIgnore]
    public string RangeDisplay => $"{FormatTime(StartTime)} – {FormatTime(EndTime)}";

    private static string FormatTime(TimeSpan t) => DateTime.Today.Add(t).ToString("h:mm tt");
}
