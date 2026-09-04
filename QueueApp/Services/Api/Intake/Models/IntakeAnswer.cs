using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Intake.Models;

// A pointer at an object in the private intake-uploads Storage bucket.
public class IntakeFileRef
{
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("content_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentType { get; set; }

    [JsonPropertyName("size_bytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? SizeBytes { get; set; }
}

// One stored answer, as it sits in the intake_responses jsonb under its field's id.
//
// The label, type and order are written alongside the value rather than looked up from
// service_intake_fields at read time: a stored answer is a snapshot of what was actually asked.
// The shop can rename a field or delete it a month later and the visit that answered the old
// question still renders the old question, which is the only reading that stays honest — and it
// keeps the visit page off the definitions table entirely.
public class IntakeAnswer
{
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("field_type")] public string FieldType { get; set; } = IntakeFieldTypes.ShortText;
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }
    [JsonPropertyName("is_required")] public bool IsRequired { get; set; }

    // Short text, long text and single select all answer with one string.
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    [JsonPropertyName("values")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Values { get; set; }

    [JsonPropertyName("file")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntakeFileRef? File { get; set; }

    [JsonIgnore] public bool IsFile => FieldType == IntakeFieldTypes.File;

    // An optional field left blank is still written, with nothing in it: the operator has to be
    // able to tell "never asked" from "asked, not answered".
    [JsonIgnore]
    public bool HasAnswer => IsFile
        ? File is not null && !string.IsNullOrWhiteSpace(File.Path)
        : Values is { Count: > 0 } || !string.IsNullOrWhiteSpace(Value);

    [JsonIgnore]
    public string DisplayText => !HasAnswer
        ? string.Empty
        : IsFile
            ? File!.Name
            : Values is { Count: > 0 }
                ? string.Join(", ", Values)
                : Value ?? string.Empty;

    // The jsonb is an object keyed by field id, so it comes back in whatever order it was stored
    // in. sort_order is the order the shop chose, and it travelled with each answer for this.
    public static IReadOnlyList<IntakeAnswer> Ordered(Dictionary<string, IntakeAnswer>? responses) =>
        responses is null or { Count: 0 }
            ? Array.Empty<IntakeAnswer>()
            : responses.Values.OrderBy(a => a.SortOrder).ToList();
}
