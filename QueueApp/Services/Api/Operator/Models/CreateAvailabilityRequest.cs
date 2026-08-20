using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

public class CreateAvailabilityRequest
{
    [JsonPropertyName("operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("day_of_week")] public int DayOfWeek { get; set; }

    [JsonPropertyName("start_time")]
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan StartTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan EndTime { get; set; }
}
