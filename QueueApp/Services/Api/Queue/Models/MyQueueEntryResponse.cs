using System.Text.Json.Serialization;
using QueueApp.Services.Api.Intake.Models;
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

    [JsonPropertyName("awaiting_collection_at")] public DateTime? AwaitingCollectionAt { get; set; }
    [JsonPropertyName("collected_at")] public DateTime? CollectedAt { get; set; }

    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }
    [JsonPropertyName("details")] public QueueEntryDetails? Details { get; set; }

    // What the customer answered on the way in, keyed by the field's id. Selected via `*`, so it
    // stays null and harmless until the column exists.
    // TODO: stub — queue_entries.intake_responses jsonb; see
    // Documentation/service-intake-fields-backend-requirements.md.
    [JsonPropertyName("intake_responses")] public Dictionary<string, IntakeAnswer>? IntakeResponses { get; set; }

    [JsonPropertyName("business")] public VisitBusinessRef? Business { get; set; }
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }

    [JsonIgnore] public Guid BusinessId => Business?.Id ?? BusinessIdColumn;
    [JsonIgnore] public string BusinessName => Business?.Name ?? "Unknown business";
    [JsonIgnore] public string Category => Business?.Category ?? "other";
    // "Any available" was the wording of a choice the customer made. join_queue assigns at join
    // time now, so a queue entry with no operator isn't a preference — it's a shop that had nobody
    // on shift when this person walked in.
    [JsonIgnore] public bool HasOperator => OperatorId is not null;
    [JsonIgnore] public string OperatorName => Operator?.DisplayName ?? "Next available";
    [JsonIgnore] public string ServiceName => Service?.Name ?? string.Empty;
    [JsonIgnore] public int? PriceCents => Service?.PriceCents;

    [JsonIgnore] public bool IsWaiting => Status == QueueEntryStatuses.Waiting;
    [JsonIgnore] public bool IsBeingServed => Status == QueueEntryStatuses.Serving;
    [JsonIgnore] public bool IsAwaitingCollection => Status == QueueEntryStatuses.AwaitingCollection;

    [JsonIgnore] public bool IsLive => IsWaiting || IsBeingServed || IsAwaitingCollection;
    [JsonIgnore] public bool IsNoShow => QueueEntryStatuses.IsNoShow(Status);
    [JsonIgnore] public bool IsCancelled => QueueEntryStatuses.IsCancelled(Status);

    // done_at is written by complete_entry alone, so it identifies a finished visit without this
    // having to know which queue_status label the enum actually spells.
    [JsonIgnore] public bool IsFinished => DoneAt is not null;

    [JsonIgnore] public DateTimeOffset JoinedAtUtc => AsUtc(JoinedAt);
    [JsonIgnore] public DateTimeOffset? ServingAtUtc => ServingAt is { } value ? AsUtc(value) : null;
    [JsonIgnore] public DateTimeOffset? DoneAtUtc => DoneAt is { } value ? AsUtc(value) : null;
    [JsonIgnore] public DateTimeOffset? AwaitingCollectionAtUtc => AwaitingCollectionAt is { } value ? AsUtc(value) : null;
    [JsonIgnore] public DateTimeOffset? CollectedAtUtc => CollectedAt is { } value ? AsUtc(value) : null;

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

    // Written by join_queue when it resolved the operator itself rather than being handed one.
    // The board reads it to tell an automatic placement from a customer who asked for that chair
    // by name, and can reshuffle the automatic ones without overriding anybody's preference.
    [JsonPropertyName("assigned")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Assigned { get; set; }

    [JsonIgnore] public bool WasAutoAssigned => Assigned == AssignedValues.Auto;

    // Carries the existing details forward: this whole object is PATCHed over the column, so
    // anything left off is deleted. PostgREST has no jsonb merge to lean on — a PATCH body is
    // plain column values — so the merge happens here, from what the caller already loaded.
    public static QueueEntryDetails CancelledByCustomer(QueueEntryDetails? existing = null) => new()
    {
        CancelledBy = CancelledByValues.Customer,
        CancelledAt = DateTimeOffset.UtcNow,
        Assigned = existing?.Assigned,
    };
}

public static class AssignedValues
{
    public const string Auto = "auto";
}
