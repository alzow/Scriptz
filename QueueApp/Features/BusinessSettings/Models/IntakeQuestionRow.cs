using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Features.BusinessSettings.Helpers;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Features.BusinessSettings.Models;

// One question as the service page lists it. The condition is resolved here rather than on the
// response, because reading it back as a sentence needs the sibling it points at.
public sealed class IntakeQuestionRow : ObservableObject
{
    public required IntakeFieldResponse Field { get; init; }

    public Guid Id => Field.Id;
    public string Prompt => Field.Label;
    public string SummaryText => Field.SummaryText;

    private string _conditionText = string.Empty;
    public string ConditionText
    {
        get => _conditionText;
        set
        {
            SetProperty(ref _conditionText, value);
            OnPropertyChanged(nameof(HasCondition));
        }
    }

    public bool HasCondition => !string.IsNullOrEmpty(ConditionText);

    public static IntakeQuestionRow From(
        IntakeFieldResponse field, IEnumerable<IntakeFieldResponse> allFields) =>
        new()
        {
            Field = field,
            ConditionText = IntakeQuestionHelper.ConditionSentence(field, allFields),
        };
}
