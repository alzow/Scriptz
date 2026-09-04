using System.Collections.ObjectModel;
using System.ComponentModel;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow.Helpers;

// The selected service's questions and the answers given to them. Owns nothing the flow's other
// steps need, so it stays out of the view model: the flow asks it what is outstanding and what to
// send, and it raises AnswersChanged when either could have moved.
public sealed class FlowIntakeCoordinator
{
    public ObservableCollection<IntakeFieldItem> Fields { get; } = new();

    public bool HasFields => Fields.Count > 0;

    public int OutstandingCount => Fields.Count(f => f.IsVisible && !f.IsSatisfied);

    public event EventHandler? AnswersChanged;

    private Dictionary<Guid, List<IntakeFieldResponse>> _fieldsByService = new();

    private readonly IIntakeFileService _intakeFileService;

    public FlowIntakeCoordinator(IIntakeFileService intakeFileService)
    {
        _intakeFileService = intakeFileService;
    }

    public void SetCatalogue(Dictionary<Guid, List<IntakeFieldResponse>> fieldsByService)
    {
        _fieldsByService = fieldsByService ?? new Dictionary<Guid, List<IntakeFieldResponse>>();
    }

    public void BuildFor(Guid serviceId)
    {
        Clear();

        if (!_fieldsByService.TryGetValue(serviceId, out var fields))
            return;

        foreach (var field in fields.OrderBy(f => f.SortOrder))
        {
            var item = IntakeFieldItem.From(field);
            item.PropertyChanged += OnFieldChanged;
            Fields.Add(item);
        }

        RecomputeVisibility();
    }

    public void Clear()
    {
        foreach (var item in Fields)
            item.PropertyChanged -= OnFieldChanged;

        Fields.Clear();
    }

    public void SelectOption(IntakeOptionItem? option)
    {
        if (option is null)
            return;

        var field = Fields.FirstOrDefault(f => f.Options.Contains(option));
        if (field is null)
            return;

        if (field.IsSingleSelect)
        {
            foreach (var candidate in field.Options)
                candidate.IsSelected = ReferenceEquals(candidate, option);
        }
        else
        {
            option.IsSelected = !option.IsSelected;
        }

        field.NotifyAnswerChanged();
        RecomputeVisibility();
        RaiseAnswersChanged();
    }

    // Only a single/multi-select answer can gate another field's visibility, so this only needs
    // rerunning where SelectOption already runs. Fields is in sort_order, and a rule may only
    // target a field earlier in that order (the editor is what enforces that), so one forward pass
    // always sees a trigger's own visibility settled before it is read here.
    private void RecomputeVisibility()
    {
        foreach (var field in Fields)
        {
            var rule = field.Field.VisibilityRule;
            var isVisible = rule is null || IsRuleSatisfied(rule);

            if (field.IsVisible == isVisible)
                continue;

            field.IsVisible = isVisible;
            if (!isVisible)
                field.ClearAnswer();
        }
    }

    private bool IsRuleSatisfied(IntakeVisibilityRule rule)
    {
        var trigger = Fields.FirstOrDefault(f => f.FieldId == rule.FieldId);
        return trigger is { IsVisible: true } &&
               trigger.SelectedOptions.Any(o => rule.Values.Contains(o.Text));
    }

    public async Task PickFileAsync(IntakeFieldItem? field, Guid serviceId)
    {
        if (field is null || field.IsPickingFile)
            return;

        field.IsPickingFile = true;
        try
        {
            var picked = await _intakeFileService.PickAndUploadAsync(serviceId, field.FieldId);

            // Null is the customer closing the picker without choosing, which is not a failure and
            // must not wipe a file they already attached.
            if (picked is null)
                return;

            field.FileAnswer = picked;
            field.NotifyAnswerChanged();
            RaiseAnswersChanged();
        }
        finally
        {
            field.IsPickingFile = false;
        }
    }

    public void ClearFile(IntakeFieldItem? field)
    {
        if (field is null)
            return;

        field.FileAnswer = null;
        field.NotifyAnswerChanged();
        RaiseAnswersChanged();
    }

    // Null unless the service actually asked something the customer could still see, so an entry
    // for a service with no fields — or with every field hidden by a condition that never matched —
    // goes up with the body it went up with before any of this existed. A field a condition is
    // currently hiding is left out entirely rather than written blank: that's the only reading that
    // stays honest about a question the customer never actually saw.
    public Dictionary<string, IntakeAnswer>? BuildResponses()
    {
        var visible = Fields.Where(f => f.IsVisible).ToList();
        return visible.Count > 0
            ? visible.ToDictionary(f => f.FieldId.ToString(), f => f.ToAnswer())
            : null;
    }

    private void OnFieldChanged(object? sender, PropertyChangedEventArgs e) => RaiseAnswersChanged();

    private void RaiseAnswersChanged() => AnswersChanged?.Invoke(this, EventArgs.Empty);
}
