using System.Text.Json.Serialization;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Services.Api.Booking.Models;

// No p_operator_id — the customer didn't (couldn't) choose one; create_booking_any assigns
// whichever resource is actually free at that slot.
public class CreateBookingAnyRequest
{
    [JsonPropertyName("p_business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("p_service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("p_customer_id")] public Guid CustomerId { get; set; }
    [JsonPropertyName("p_starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("p_note")] public string? Note { get; set; }

    // Same deal as join_queue's: absent from the body unless the service asked something.
    // TODO: stub — the create_booking RPCs need a p_intake_responses jsonb parameter; see
    // Documentation/service-intake-fields-backend-requirements.md.
    [JsonPropertyName("p_intake_responses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, IntakeAnswer>? IntakeResponses { get; set; }
}
