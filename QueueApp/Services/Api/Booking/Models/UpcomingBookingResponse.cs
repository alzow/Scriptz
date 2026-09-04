using System.Globalization;
using System.Text.Json.Serialization;
using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Services.Api.Booking.Models;

public class UpcomingBookingBusinessRef
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "other";
    [JsonPropertyName("allow_operator_choice")] public bool AllowOperatorChoice { get; set; } = true;
}

public class UpcomingBookingResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("started_at")] public DateTimeOffset? StartedAt { get; set; }
    [JsonPropertyName("awaiting_collection_at")] public DateTimeOffset? AwaitingCollectionAt { get; set; }
    [JsonPropertyName("collected_at")] public DateTimeOffset? CollectedAt { get; set; }
    [JsonPropertyName("business")] public UpcomingBookingBusinessRef? Business { get; set; }
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("details")] public BookingDetails? Details { get; set; }

    // Same as the queue side's: selected via `*` on the visit page's read, absent everywhere else.
    // TODO: stub — bookings.intake_responses jsonb; see
    // Documentation/service-intake-fields-backend-requirements.md.
    [JsonPropertyName("intake_responses")] public Dictionary<string, IntakeAnswer>? IntakeResponses { get; set; }

    // Stamped on when the booking settled — completed, cancelled or no-show. One model serves both
    // the upcoming list and the history list, and it needs no branch between them: an upcoming
    // booking has no snapshot yet and falls through to the live embeds, which is what a booking
    // still ahead of the customer should show. Selected via `*`, so they stay null and harmless
    // until the columns exist.
    // TODO: stub — bookings.service_name / service_price_cents / operator_name; see
    // Documentation/historic-snapshot-backend-requirements.md.
    [JsonPropertyName("service_name")] public string? ServiceNameColumn { get; set; }
    [JsonPropertyName("service_price_cents")] public int? ServicePriceCentsColumn { get; set; }
    [JsonPropertyName("operator_name")] public string? OperatorNameColumn { get; set; }

    [JsonIgnore] public bool IsCancelling { get; set; }

    [JsonIgnore] public Guid BusinessId => Business?.Id ?? Guid.Empty;
    [JsonIgnore] public string BusinessName => Business?.Name ?? "";

    [JsonIgnore]
    public bool HasOperator => Operator is not null || !string.IsNullOrWhiteSpace(OperatorNameColumn);

    [JsonIgnore]
    public string OperatorName =>
        TextFormat.FirstNonBlank(OperatorNameColumn, Operator?.DisplayName)
        ?? VisitSnapshotDefaults.BookingOperatorName;

    [JsonIgnore]
    public string ServiceName =>
        TextFormat.FirstNonBlank(ServiceNameColumn, Service?.Name) ?? "";

    [JsonIgnore] public int? PriceCents => ServicePriceCentsColumn ?? Service?.PriceCents;
    [JsonIgnore] public string Category => Business?.Category ?? "other";
    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);
    [JsonIgnore] public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    // Why the business called it off, if they gave a reason. Without it a cancellation is just a
    // booking that vanished.
    [JsonIgnore] public string? CancellationReason => Details?.CancellationReason;
    [JsonIgnore] public string? CancelledBy => Details?.CancelledBy;
    [JsonIgnore] public DateTimeOffset? CancelledAt => Details?.CancelledAt;
    [JsonIgnore] public bool WasCancelledByCustomer => CancelledBy == CancelledByValues.Customer;
    [JsonIgnore] public string PriceText => MoneyFormat.Format(PriceCents);
    [JsonIgnore] public bool HasCancellationReason => !string.IsNullOrWhiteSpace(CancellationReason);
    [JsonIgnore] public string CancellationReasonText => $"Cancelled — {CancellationReason}";

    // "with Bay 3" reads as useful detail at a barbershop, noise at a car wash the customer never
    // got to choose a bay at — omit the operator clause entirely for pooled businesses.
    [JsonIgnore]
    public string ScheduleText => Business?.AllowOperatorChoice == false
        ? $"{TimeText} · {ServiceName}"
        : $"{TimeText} · {ServiceName} with {OperatorName}";

    [JsonIgnore]
    private DateTimeOffset LocalStart => StartsAt.ToOffset(TimeSpan.FromHours(2));

    [JsonIgnore]
    public string DateTimeDisplay => LocalStart.ToString("ddd d MMM, h:mm tt");

    // Day.ToString(), not ToString("d") — a lone "d" is the standard short-date specifier, so on a
    // za-ZA device the date rail read "2026/08/28" instead of "28". Invariant so the number is
    // digits whatever the device's culture is.
    [JsonIgnore] public string DayText => LocalStart.Day.ToString(CultureInfo.InvariantCulture);
    [JsonIgnore] public string MonthText => LocalStart.ToString("MMM").ToUpperInvariant();
    [JsonIgnore] public string TimeText => LocalStart.ToString("h:mm tt");

    [JsonIgnore]
    public string EffectiveStatus => Status == "confirmed" && EndsAt < DateTimeOffset.UtcNow ? "expired" : Status;

    [JsonIgnore] public bool IsAwaitingCollection => Status == BookingStatuses.AwaitingCollection;
    [JsonIgnore] public bool IsCancellable => Status is "pending" or "confirmed";
    [JsonIgnore] public string StatusLabel => Status switch
    {
        "pending" => "Pending",
        _ when IsAwaitingCollection => "Ready for collection",
        _ => "Confirmed",
    };
}
