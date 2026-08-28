using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Booking.Models;

public class BookingResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("operator_id")] public Guid OperatorId { get; set; }
    [JsonPropertyName("service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("customer_id")] public Guid? CustomerId { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "pending";
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }
    [JsonPropertyName("details")] public object? Details { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
}
