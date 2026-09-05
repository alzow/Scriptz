using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Intake.Models;

// Shown only when the single/multi-select question identified by FieldId has one of Values
// selected. Null on IntakeFieldResponse means the question is always asked. Kept as one jsonb
// object (service_intake_fields.visibility_rule) rather than two plain columns so there is no
// partial state to null out, and so a later "not_in"/multi-condition shape can land without a
// migration.
public class IntakeVisibilityRule
{
    [JsonPropertyName("field_id")] public Guid FieldId { get; set; }
    [JsonPropertyName("values")] public List<string> Values { get; set; } = new();
}

// One question a service asks before the entry is created. Rows live in service_intake_fields,
// which does not exist yet — see Documentation/service-intake-fields-backend-requirements.md.
//
// TODO: stub — table, columns and RLS are specified in that file, not applied to Supabase.
public class IntakeFieldResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("field_type")] public string FieldType { get; set; } = IntakeFieldTypes.ShortText;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;

    // Shown under the field on the intake step. A registration number the shop can read back is
    // worth more than a phone call, and an example is what gets one.
    [JsonPropertyName("hint")] public string? Hint { get; set; }

    [JsonPropertyName("is_required")] public bool IsRequired { get; set; }
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }

    // Only meaningful for the two select types; null everywhere else.
    [JsonPropertyName("options")] public List<string>? Options { get; set; }

    // Only meaningful for a field whose FieldId points at an earlier single/multi-select question
    // in the same service — the editor enforces that, this shape doesn't.
    [JsonPropertyName("visibility_rule")] public IntakeVisibilityRule? VisibilityRule { get; set; }

    [JsonIgnore] public bool HasOptions => IntakeFieldTypes.HasOptions(FieldType);
    [JsonIgnore] public bool HasHint => !string.IsNullOrWhiteSpace(Hint);
    [JsonIgnore] public string TypeDisplay => IntakeFieldTypes.DisplayName(FieldType);

    // What the settings list shows under the prompt. The condition is not in here: it reads as a
    // sentence naming the question it points at, which needs the rest of the list to resolve.
    [JsonIgnore] public string SummaryText =>
        IsRequired ? $"{TypeDisplay} · Required" : $"{TypeDisplay} · Optional";

    [JsonIgnore] public string OptionsText => Options is { Count: > 0 }
        ? string.Join(", ", Options)
        : string.Empty;
}
