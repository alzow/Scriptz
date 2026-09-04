using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Services.Api.Booking.Models;

public class AgendaCustomerRef
{
    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
}

public class AgendaOperatorRef
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
}

public class AgendaServiceRef
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("price_cents")] public int? PriceCents { get; set; }
    [JsonPropertyName("est_minutes")] public int EstMinutes { get; set; }
}

// bookings has no customer_name column (queue_entries does), and profiles is readable only by its
// own owner, so an embedded customer:profiles(display_name) comes back null for every booking a
// business didn't make itself. Operator-created bookings therefore carry the name they were taken
// with in the details jsonb, which the owner can always read. See
// Documentation/STEP-18-BOOKING-AGENDA-SUPABASE.md §3.
public class BookingDetails
{
    [JsonPropertyName("customer_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customer_phone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomerPhone { get; set; }

    [JsonPropertyName("created_by")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CreatedBy { get; set; }

    // Why the business called it off. bookings has no column for this, but details is jsonb and the
    // owner-update policy already covers writing it, so no migration is needed to give the customer
    // a reason instead of a silent disappearance.
    [JsonPropertyName("cancellation_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CancellationReason { get; set; }

    // Who called it off, and when. bookings has neither column, and a cancellation the customer
    // made themselves must never read as "the shop cancelled on you", so both live here alongside
    // the reason.
    [JsonPropertyName("cancelled_by")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CancelledBy { get; set; }

    [JsonPropertyName("cancelled_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CancelledAt { get; set; }

    // A PATCH replaces the whole jsonb value, so anything already in there has to be carried across
    // or it is silently dropped.
    public static BookingDetails WithCancellationReason(BookingDetails? existing, string? reason) => new()
    {
        CustomerName = existing?.CustomerName,
        CustomerPhone = existing?.CustomerPhone,
        CreatedBy = existing?.CreatedBy,
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
        CancelledBy = CancelledByValues.Business,
        CancelledAt = DateTimeOffset.UtcNow,
    };

    public static BookingDetails CancelledByCustomer(BookingDetails? existing) => new()
    {
        CustomerName = existing?.CustomerName,
        CustomerPhone = existing?.CustomerPhone,
        CreatedBy = existing?.CreatedBy,
        CancellationReason = existing?.CancellationReason,
        CancelledBy = CancelledByValues.Customer,
        CancelledAt = DateTimeOffset.UtcNow,
    };
}

public partial class AgendaBookingResponse : ObservableObject
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("service_id")] public Guid? ServiceId { get; set; }
    [JsonPropertyName("customer_id")] public Guid? CustomerId { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("operator")] public AgendaOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public AgendaServiceRef? Service { get; set; }
    [JsonPropertyName("customer")] public AgendaCustomerRef? Customer { get; set; }

    [JsonPropertyName("customer_name")] public string? CustomerNameColumn { get; set; }
    [JsonPropertyName("customer_phone")] public string? CustomerPhoneColumn { get; set; }

    // What the booking was settled as, and at what price. The day's revenue is summed off this, so
    // reading it live meant a price change today silently rewrote what yesterday earned. A booking
    // still pending or confirmed carries no snapshot and reads live — it counts towards today's
    // revenue at today's price, which is the figure the shop is actually expecting. Selected via
    // `*`, so they stay null and harmless until the columns exist.
    // TODO: stub — bookings.service_name / service_price_cents / service_est_minutes /
    // operator_name; see Documentation/historic-snapshot-backend-requirements.md.
    [JsonPropertyName("service_name")] public string? ServiceNameColumn { get; set; }
    [JsonPropertyName("service_price_cents")] public int? ServicePriceCentsColumn { get; set; }
    [JsonPropertyName("service_est_minutes")] public int? ServiceEstMinutesColumn { get; set; }
    [JsonPropertyName("operator_name")] public string? OperatorNameColumn { get; set; }

    [JsonPropertyName("details")] public BookingDetails? Details { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }

    // Whatever the customer (or the operator taking the call) wrote about this booking — a vehicle
    // registration, what's actually wrong with the car. The bookings.note column already exists and
    // create_booking already accepts it as p_note.
    [JsonPropertyName("note")] public string? Note { get; set; }

    // When the work actually began, as opposed to when it was scheduled to. Queue mode has
    // serving_at; bookings has no equivalent column yet, so this stays null on today's schema and
    // the elapsed counter falls back to counting against the schedule. Selected via `*`, never by
    // name, so the query keeps working until the column exists.
    [JsonPropertyName("started_at")] public DateTimeOffset? StartedAt { get; set; }

    [JsonPropertyName("awaiting_collection_at")] public DateTimeOffset? AwaitingCollectionAt { get; set; }

    // What the customer answered when they made the booking. The agenda read selects `*`, so this
    // arrives with no query change; each answer carries its own label, type and order, so the
    // agenda renders it without reading service_intake_fields.
    [JsonPropertyName("intake_responses")] public Dictionary<string, IntakeAnswer>? IntakeResponses { get; set; }

    [JsonIgnore] public bool IsConfirming { get; set; }
    [JsonIgnore] public bool IsCompleting { get; set; }
    [JsonIgnore] public bool IsCancelling { get; set; }
    [JsonIgnore] public bool IsMarkingNoShow { get; set; }
    [JsonIgnore] public bool IsSavingProgress { get; set; }

    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);
    [JsonIgnore] public bool HasNote => !string.IsNullOrWhiteSpace(Note);
    [JsonIgnore] public bool HasIntakeAnswers => IntakeResponses is { Count: > 0 };
    [JsonIgnore] public string? CancellationReason => Details?.CancellationReason;
    [JsonIgnore] public bool HasCancellationReason => !string.IsNullOrWhiteSpace(CancellationReason);

    [JsonIgnore]
    public string OperatorName =>
        TextFormat.FirstNonBlank(OperatorNameColumn, Operator?.DisplayName)
        ?? VisitSnapshotDefaults.BookingOperatorName;

    [JsonIgnore]
    public string ServiceName =>
        TextFormat.FirstNonBlank(ServiceNameColumn, Service?.Name) ?? "";

    [JsonIgnore]
    public int ServiceMinutes =>
        ServiceEstMinutesColumn ?? Service?.EstMinutes ?? (int)(EndsAt - StartsAt).TotalMinutes;

    [JsonIgnore] public int? PriceCents => ServicePriceCentsColumn ?? Service?.PriceCents;
    [JsonIgnore] public string PriceText => MoneyFormat.Format(PriceCents);

    [JsonIgnore]
    public string CustomerName =>
        CustomerNameColumn
        ?? Customer?.DisplayName
        ?? Details?.CustomerName
        ?? "Customer";

    [JsonIgnore]
    public string? CustomerPhone =>
        CustomerPhoneColumn
        ?? Customer?.Phone
        ?? Details?.CustomerPhone;
    [JsonIgnore] public bool HasPhone => !string.IsNullOrWhiteSpace(CustomerPhone);

    [JsonIgnore] public DateTimeOffset LocalStart => StartsAt.ToOffset(LocalOffset);
    [JsonIgnore] public DateTimeOffset LocalEnd => EndsAt.ToOffset(LocalOffset);

    // SA has no DST — the same fixed +2 assumption every other booking model and the slot engine make.
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(2);

    [JsonIgnore] public string TimeText => LocalStart.ToString("HH:mm");
    [JsonIgnore] public string DurationText => FormatDuration(LocalEnd - LocalStart);
    [JsonIgnore] public string TimeRangeDisplay => $"{LocalStart:HH:mm} – {LocalEnd:HH:mm}";
    [JsonIgnore] public string DayAndRangeDisplay => $"{LocalStart:ddd d} · {TimeRangeDisplay}";

    [JsonIgnore] public string Initials => TextFormat.Initials(CustomerName);

    [JsonIgnore] public bool IsPending => Status == BookingStatuses.Pending;
    [JsonIgnore] public bool IsConfirmed => Status == BookingStatuses.Confirmed;
    [JsonIgnore] public bool IsCompleted => Status == BookingStatuses.Completed;
    [JsonIgnore] public bool IsCancelled => Status == BookingStatuses.Cancelled;
    [JsonIgnore] public bool IsNoShow => Status == BookingStatuses.NoShow;
    [JsonIgnore] public bool IsAwaitingCollection => Status == BookingStatuses.AwaitingCollection;

    // Two ways in, because only one of them can exist on the current schema and neither is
    // guaranteed: the enum value if the migration added it, or a started_at stamp on an otherwise
    // confirmed booking. With neither, nothing ever reads as in progress — which is the truth.
    [JsonIgnore]
    public bool IsInProgress =>
        Status == BookingStatuses.InProgress || (StartedAt.HasValue && IsConfirmed);

    [JsonIgnore] public bool IsFinished => IsCompleted || IsCancelled || IsNoShow;

    [JsonIgnore] public bool IsWithinWindow => DateTimeOffset.UtcNow >= StartsAt && DateTimeOffset.UtcNow < EndsAt;
    [JsonIgnore] public bool HasWindowPassed => DateTimeOffset.UtcNow >= EndsAt;

    [JsonIgnore] public bool CanConfirm => IsPending;
    [JsonIgnore] public bool CanComplete => !IsFinished && !IsAwaitingCollection && (IsWithinWindow || HasWindowPassed);
    [JsonIgnore] public bool CanMarkNoShow => !IsFinished && !IsAwaitingCollection && IsWithinWindow;
    [JsonIgnore] public bool CanUpdateCustomer => !IsFinished && !IsAwaitingCollection && IsWithinWindow;
    [JsonIgnore] public bool CanCancel => !IsFinished && !IsAwaitingCollection;
    [JsonIgnore] public bool CanMarkCollected => IsAwaitingCollection;

    [JsonIgnore] public DateTimeOffset ElapsedFrom => (StartedAt ?? StartsAt).ToOffset(LocalOffset);

    public static string FormatDuration(TimeSpan span)
    {
        var minutes = (int)Math.Round(span.TotalMinutes);
        if (minutes < 60) return $"{minutes}m";

        var hours = minutes / 60;
        var rest = minutes % 60;
        return rest == 0 ? $"{hours}h" : $"{hours}h {rest}m";
    }
}
