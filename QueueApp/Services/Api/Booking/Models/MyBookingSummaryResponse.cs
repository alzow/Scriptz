using System.Text.Json.Serialization;
using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Services.Api.Booking.Models;

public class MyBookingSummaryResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }

    // Stamped on when the booking settled; null while it is still pending or confirmed, which is
    // when the live embeds below are the right answer.
    // TODO: stub — bookings.service_name / service_price_cents / operator_name; see
    // Documentation/historic-snapshot-backend-requirements.md.
    [JsonPropertyName("service_name")] public string? ServiceNameColumn { get; set; }
    [JsonPropertyName("service_price_cents")] public int? ServicePriceCentsColumn { get; set; }
    [JsonPropertyName("operator_name")] public string? OperatorNameColumn { get; set; }

    [JsonIgnore] public bool IsCancelling { get; set; }

    [JsonIgnore]
    public string OperatorName =>
        TextFormat.FirstNonBlank(OperatorNameColumn, Operator?.DisplayName)
        ?? VisitSnapshotDefaults.BookingOperatorName;

    [JsonIgnore]
    public string ServiceName =>
        TextFormat.FirstNonBlank(ServiceNameColumn, Service?.Name) ?? "";

    [JsonIgnore] public int? PriceCents => ServicePriceCentsColumn ?? Service?.PriceCents;
    [JsonIgnore] public string PriceText => MoneyFormat.Format(PriceCents);
    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);

    [JsonIgnore]
    private DateTimeOffset LocalStart => StartsAt.ToOffset(TimeSpan.FromHours(2));

    [JsonIgnore]
    public string DateTimeDisplay => LocalStart.ToString("ddd d MMM, h:mm tt");

    [JsonIgnore]
    public bool IsCancellable => Status is "pending" or "confirmed";

    [JsonIgnore]
    public string StatusLabel => Status switch
    {
        "pending" => "Pending",
        "confirmed" => "Confirmed",
        "cancelled" => "Cancelled",
        "completed" => "Completed",
        _ => Status
    };
}
