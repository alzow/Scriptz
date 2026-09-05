using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Intake.Models;

// Editing a field never touches answers already stored against it: those carry their own copy of
// the question they were asked (see IntakeAnswer).
public class UpdateIntakeFieldRequest
{
    [JsonPropertyName("field_type")] public string FieldType { get; set; } = IntakeFieldTypes.ShortText;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("is_required")] public bool IsRequired { get; set; }

    // Written even when null: clearing the hint has to reach the row, and this is a full-field
    // update for the same reason UpdateServiceRequest is.
    [JsonPropertyName("hint")] public string? Hint { get; set; }

    [JsonPropertyName("options")] public List<string>? Options { get; set; }

    [JsonPropertyName("visibility_rule")] public IntakeVisibilityRule? VisibilityRule { get; set; }
}
