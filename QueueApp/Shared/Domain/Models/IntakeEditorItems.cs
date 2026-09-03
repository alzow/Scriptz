using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Shared.Domain.Models;

// The five types, as a row of chips in the field editor.
public sealed class IntakeTypeOption : ObservableObject
{
    public required string FieldType { get; init; }
    public required string Name { get; init; }
    public bool IsSelected { get; set; }

    public static IntakeTypeOption From(string fieldType) => new()
    {
        FieldType = fieldType,
        Name = IntakeFieldTypes.DisplayName(fieldType),
    };
}

// One choice being edited on a select-type field, before it becomes a string in options.
public sealed class IntakeOptionRow : ObservableObject
{
    public required string Text { get; set; }
}
