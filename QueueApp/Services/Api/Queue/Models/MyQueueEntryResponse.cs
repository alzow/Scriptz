using System.Text.Json.Serialization;
using QueueApp.Shared.Domain;

namespace QueueApp.Services.Api.Queue.Models;

// One of the customer's own queue entries, with the business, operator and service it was joined
// against embedded. This is what History lists and what VisitPage loads: `visits` carries only a
// visited_at, so nothing built off it can say how long anyone waited.
public class MyQueueEntryResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessIdColumn { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("service_id")] public Guid? ServiceId { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("joined_at")] public DateTime JoinedAt { get; set; }
    [JsonPropertyName("serving_at")] public DateTime? ServingAt { get; set; }
    [JsonPropertyName("done_at")] public DateTime? DoneAt { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }
    [JsonPropertyName("details")] public QueueEntryDetails? Details { get; set; }

    [JsonPropertyName("business")] public VisitBusinessRef? Business { get; set; }
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }

    [JsonIgnore] public Guid BusinessId => Business?.Id ?? BusinessIdColumn;
    [JsonIgnore] public string BusinessName => Business?.Name ?? "Unknown business";
    [JsonIgnore] public string Category => Business?.Category ?? "other";
    [JsonIgnore] public string OperatorName => Operator?.DisplayName ?? "Any available";
    [JsonIgnore] public string ServiceName => Service?.Name ?? string.Empty;
    [JsonIgnore] public int? PriceCents => Service?.PriceCents;

    [JsonIgnore] public bool IsWaiting => Status == QueueEntryStatuses.Waiting;
    [JsonIgnore] public bool IsBeingServed => Status == QueueEntryStatuses.Serving;
    [JsonIgnore] public bool IsLive => IsWaiting || IsBeingServed;
    [JsonIgnore] public bool IsNoShow => QueueEntryStatuses.IsNoShow(Status);
    [JsonIgnore] public bool IsCancelled => QueueEntryStatuses.IsCancelled(Status);

    // done_at is written by complete_entry alone, so it identifies a finished visit without this
    // having to know which queue_status label the enum actually spells.
    [JsonIgnore] public bool IsFinished => DoneAt is not null;

    [JsonIgnore] public DateTimeOffset JoinedAtUtc => AsUtc(JoinedAt);
    [JsonIgnore] public DateTimeOffset? ServingAtUtc => ServingAt is { } value ? AsUtc(value) : null;
    [JsonIgnore] public DateTimeOffset? DoneAtUtc => DoneAt is { } value ? AsUtc(value) : null;

    // The customer's own leave-the-queue stamp, written into details because queue_entries has no
    // cancelled_at column. Absent on a row cancelled by the shop or by an older build.
    [JsonIgnore] public DateTimeOffset? CancelledAtUtc => Details?.CancelledAt;

    // The columns are timestamptz, so the JSON carries an offset — and System.Text.Json spends it
    // on the way in, handing back a Local DateTime on the device's clock. Stamping that Utc shifted
    // every queue time by the phone's offset; only a naive value is ours to label.
    private static DateTimeOffset AsUtc(DateTime value) => value.Kind == DateTimeKind.Unspecified
        ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
        : new DateTimeOffset(value).ToUniversalTime();
}

// queue_entries.details is jsonb and unused by the queue engine, so the two facts a cancellation
// needs — who called it off and when — go in there rather than waiting on a migration. Same trick
// the booking side already uses for its cancellation reason.
public class QueueEntryDetails
{
    [JsonPropertyName("cancelled_by")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CancelledBy { get; set; }

    [JsonPropertyName("cancelled_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CancelledAt { get; set; }

    public static QueueEntryDetails CancelledByCustomer() => new()
    {
        CancelledBy = CancelledByValues.Customer,
        CancelledAt = DateTimeOffset.UtcNow,
    };
}
