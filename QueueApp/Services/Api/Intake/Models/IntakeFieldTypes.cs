namespace QueueApp.Services.Api.Intake.Models;

// The five types a business owner can define. Deliberately short: date, number and the rest wait
// for a real use case to ask for them, because every type here is a field view the intake step has
// to render and a shape the stored answer has to carry.
public static class IntakeFieldTypes
{
    public const string ShortText = "short_text";
    public const string LongText = "long_text";
    public const string File = "file";
    public const string SingleSelect = "single_select";
    public const string MultiSelect = "multi_select";

    public static readonly string[] All =
    {
        ShortText,
        LongText,
        SingleSelect,
        MultiSelect,
        File,
    };

    // options is only meaningful for these two — the editor hides the options list for the rest.
    public static bool HasOptions(string? fieldType) =>
        fieldType is SingleSelect or MultiSelect;

    // What the owner is asked for, not what the column stores.
    public static string DisplayName(string? fieldType) => fieldType switch
    {
        ShortText => "Short text",
        LongText => "Long text",
        File => "File upload",
        SingleSelect => "Choose one",
        MultiSelect => "Choose several",
        _ => "Unknown",
    };

    // The same thing, short enough for a 44pt chip.
    public static string ChipName(string? fieldType) => fieldType switch
    {
        File => "File",
        _ => DisplayName(fieldType),
    };
}
