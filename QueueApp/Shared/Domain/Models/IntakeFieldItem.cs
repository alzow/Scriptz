using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Shared.Domain.Models;

public sealed class IntakeOptionItem : ObservableObject
{
    public required string Text { get; init; }
    public bool IsSelected { get; set; }
}

// One question on the intake step, with whatever the customer has put against it so far. Lives in
// the flow's own view model state for exactly as long as the flow does: the answers ride to the
// confirm step in memory and go up with the entry, so nothing downstream is handed a new payload.
public sealed class IntakeFieldItem : ObservableObject
{
    public required IntakeFieldResponse Field { get; init; }

    public Guid FieldId => Field.Id;
    public string Label => Field.Label;
    public bool IsRequired => Field.IsRequired;
    public string RequirementText => IsRequired ? "REQUIRED" : "OPTIONAL";

    public bool IsShortText => Field.FieldType == IntakeFieldTypes.ShortText;
    public bool IsLongText => Field.FieldType == IntakeFieldTypes.LongText;
    public bool IsFile => Field.FieldType == IntakeFieldTypes.File;
    public bool IsSingleSelect => Field.FieldType == IntakeFieldTypes.SingleSelect;
    public bool IsMultiSelect => Field.FieldType == IntakeFieldTypes.MultiSelect;

    // Both select types render the same chips; only what a tap does to the others differs.
    public ObservableCollection<IntakeOptionItem> Options { get; } = new();

    public string TextAnswer { get; set; } = string.Empty;
    public IntakeFileRef? FileAnswer { get; set; }
    public bool IsPickingFile { get; set; }

    public bool HasFile => FileAnswer is not null;
    public string FileName => FileAnswer?.Name ?? string.Empty;
    public string FileSizeText => FileAnswer?.SizeBytes is { } bytes
        ? bytes < 1024 * 1024
            ? $"{Math.Max(1, bytes / 1024)} KB"
            : $"{bytes / (1024m * 1024m):0.#} MB"
        : string.Empty;

    public string FilePickText => HasFile ? "Replace" : "Choose a file";

    public IEnumerable<IntakeOptionItem> SelectedOptions => Options.Where(o => o.IsSelected);

    public bool HasAnswer => Field.FieldType switch
    {
        IntakeFieldTypes.File => HasFile,
        IntakeFieldTypes.SingleSelect or IntakeFieldTypes.MultiSelect => SelectedOptions.Any(),
        _ => !string.IsNullOrWhiteSpace(TextAnswer),
    };

    // What keeps the footer's CTA off until the questions that matter are answered.
    public bool IsSatisfied => !IsRequired || HasAnswer;

    public static IntakeFieldItem From(IntakeFieldResponse field)
    {
        var item = new IntakeFieldItem { Field = field };

        if (field.HasOptions && field.Options is { Count: > 0 })
        {
            foreach (var option in field.Options)
                item.Options.Add(new IntakeOptionItem { Text = option });
        }

        return item;
    }

    // Selection lives on the option rows, so the parent's own derived state has to be told.
    public void NotifyAnswerChanged()
    {
        OnPropertyChanged(nameof(HasAnswer));
        OnPropertyChanged(nameof(IsSatisfied));
        OnPropertyChanged(nameof(HasFile));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(FileSizeText));
        OnPropertyChanged(nameof(FilePickText));
    }

    // The stored answer is a snapshot of the question as well as the answer — see IntakeAnswer for
    // why. Written for every defined field, answered or not, so the shop can tell a question that
    // was skipped from one that was never asked.
    public IntakeAnswer ToAnswer() => new()
    {
        Label = Field.Label,
        FieldType = Field.FieldType,
        SortOrder = Field.SortOrder,
        IsRequired = Field.IsRequired,
        Value = Field.FieldType switch
        {
            IntakeFieldTypes.File or IntakeFieldTypes.MultiSelect => null,
            IntakeFieldTypes.SingleSelect => SelectedOptions.FirstOrDefault()?.Text,
            _ => string.IsNullOrWhiteSpace(TextAnswer) ? null : TextAnswer.Trim(),
        },
        Values = Field.FieldType == IntakeFieldTypes.MultiSelect && SelectedOptions.Any()
            ? SelectedOptions.Select(o => o.Text).ToList()
            : null,
        File = Field.FieldType == IntakeFieldTypes.File ? FileAnswer : null,
    };
}
