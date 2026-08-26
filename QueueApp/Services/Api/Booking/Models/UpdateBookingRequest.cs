using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

// A PostgREST PATCH body. Every field is omitted when null so one shape can serve "start it",
// "mark it a no-show" and "move it" without a null clobbering a column it never meant to touch.
public class UpdateBookingRequest
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    [JsonPropertyName("started_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? StartedAt { get; set; }

    [JsonPropertyName("operator_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? OperatorId { get; set; }

    [JsonPropertyName("starts_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? StartsAt { get; set; }

    [JsonPropertyName("ends_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? EndsAt { get; set; }
}
