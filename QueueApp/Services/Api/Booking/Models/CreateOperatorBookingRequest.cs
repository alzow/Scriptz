using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

// A booking the shop took itself, over the phone or at the counter. Inserted straight into the
// table rather than through create_booking, which needs a customer_id there is nobody to supply.
//
// customer_id is deliberately absent: null is the honest value, and it's why the sheet asks for a
// phone number — with no account there is no reminder and no notification, only whatever the
// operator writes down here.
public class CreateOperatorBookingRequest
{
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }

    // The shop created it, so there is nobody left to confirm with.
    [JsonPropertyName("status")] public string Status { get; set; } = BookingStatuses.Confirmed;

    [JsonPropertyName("details")] public BookingDetails Details { get; set; } = new();

    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; set; }
}
