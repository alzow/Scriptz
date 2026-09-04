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

    public int OutstandingCount => Fields.Count(f => !f.IsSatisfied);

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
        RaiseAnswersChanged();
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

    // Null unless the service actually asked something, so an entry for a service with no fields
    // goes up with the body it went up with before any of this existed. Every defined field is
    // written, optional ones left blank included, so the shop can tell a question that was skipped
    // from one that was never asked.
    public Dictionary<string, IntakeAnswer>? BuildResponses() => HasFields
        ? Fields.ToDictionary(f => f.FieldId.ToString(), f => f.ToAnswer())
        : null;

    private void OnFieldChanged(object? sender, PropertyChangedEventArgs e) => RaiseAnswersChanged();

    private void RaiseAnswersChanged() => AnswersChanged?.Invoke(this, EventArgs.Empty);
}
