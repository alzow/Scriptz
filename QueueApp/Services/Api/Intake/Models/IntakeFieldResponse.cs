using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Intake.Models;

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
    [JsonPropertyName("is_required")] public bool IsRequired { get; set; }
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }

    // Only meaningful for the two select types; null everywhere else.
    [JsonPropertyName("options")] public List<string>? Options { get; set; }

    [JsonIgnore] public bool HasOptions => IntakeFieldTypes.HasOptions(FieldType);
    [JsonIgnore] public string TypeDisplay => IntakeFieldTypes.DisplayName(FieldType);

    // What the settings list shows under the label: the type, and whether it blocks joining.
    [JsonIgnore] public string SummaryText => IsRequired
        ? $"{TypeDisplay} · Required"
        : $"{TypeDisplay} · Optional";

    [JsonIgnore] public string OptionsText => Options is { Count: > 0 }
        ? string.Join(", ", Options)
        : string.Empty;
}
