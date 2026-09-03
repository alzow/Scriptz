using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Intake.Models;

public class CreateIntakeFieldRequest
{
    [JsonPropertyName("service_id")] public Guid ServiceId { get; set; }
    [JsonPropertyName("field_type")] public string FieldType { get; set; } = IntakeFieldTypes.ShortText;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("is_required")] public bool IsRequired { get; set; }
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }

    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Options { get; set; }
}
