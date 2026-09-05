using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Features.BusinessSettings.Constants;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Features.BusinessSettings.Helpers;

// When a question is asked: always, or only when an earlier answer matches. Owns which earlier
// questions may be pointed at and what the sentence currently reads as. Picks nothing itself — the
// view model runs the choosers and hands the answer back.
public sealed class IntakeRuleEditor : ObservableObject
{
    public bool HasCondition { get; set; }
    public bool IsAlways => !HasCondition;

    public string QuestionText { get; set; } = IntakeQuestionConstants.RulePickQuestionPlaceholder;
    public string ValueText { get; set; } = IntakeQuestionConstants.RulePickValuePlaceholder;

    // Nothing to be conditional on until an earlier single/multi-select question exists.
    public bool HasCandidates => _candidates.Count > 0;

    public string[] QuestionLabels => _candidates.Select(f => f.Label).ToArray();
    public string[] ValueOptions => _selectedTrigger?.Options?.ToArray() ?? Array.Empty<string>();
    public string SelectedQuestionLabel => _selectedTrigger?.Label ?? string.Empty;

    public bool IsIncomplete => HasCondition && (_selectedTrigger is null || _selectedValues.Count == 0);

    private List<IntakeFieldResponse> _candidates = new();
    private IntakeFieldResponse? _selectedTrigger;
    private List<string> _selectedValues = new();

    // A rule can only point at an earlier single/multi-select question in the same service: earlier
    // so the answer already exists by the time this one would show, and single/multi-select because
    // those are the only types with a fixed set of values to match against.
    public void Build(IEnumerable<IntakeFieldResponse> existingFields, int currentSortOrder, Guid? excludingId)
    {
        _candidates = existingFields
            .Where(f => f.Id != excludingId)
            .Where(f => f.SortOrder < currentSortOrder)
            .Where(f => IntakeFieldTypes.HasOptions(f.FieldType))
            .OrderBy(f => f.SortOrder)
            .ToList();

        OnPropertyChanged(nameof(HasCandidates));
    }

    public void Load(IntakeVisibilityRule? rule)
    {
        if (rule is null)
            return;

        HasCondition = true;
        OnPropertyChanged(nameof(IsAlways));

        _selectedTrigger = _candidates.FirstOrDefault(f => f.Id == rule.FieldId);
        _selectedValues = rule.Values.ToList();
        RaiseSentence();
    }

    public void SetConditional(bool conditional)
    {
        HasCondition = conditional;
        OnPropertyChanged(nameof(IsAlways));
    }

    public void SelectQuestion(string label)
    {
        _selectedTrigger = _candidates.FirstOrDefault(f => f.Label == label);

        // The values belong to the question that was just replaced, so they cannot survive it.
        _selectedValues = new List<string>();
        RaiseSentence();
    }

    // One value, which is what the sentence reads as. A rule stored with several still evaluates
    // and still shows here joined; re-picking replaces the set rather than editing it.
    public void SelectValue(string value)
    {
        _selectedValues = new List<string> { value };
        RaiseSentence();
    }

    public IntakeVisibilityRule? ToRule() =>
        HasCondition && _selectedTrigger is not null && _selectedValues.Count > 0
            ? new IntakeVisibilityRule { FieldId = _selectedTrigger.Id, Values = _selectedValues.ToList() }
            : null;

    public void RaiseSentence()
    {
        QuestionText = _selectedTrigger is null
            ? IntakeQuestionConstants.RulePickQuestionPlaceholder
            : IntakeQuestionHelper.TrimPrompt(_selectedTrigger.Label);

        ValueText = _selectedValues.Count == 0
            ? IntakeQuestionConstants.RulePickValuePlaceholder
            : string.Join(" or ", _selectedValues);
    }
}
