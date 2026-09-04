using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;
using QueueApp.Shared.Domain.Models;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.BusinessSettings.AddEditIntakeField;

// One question on one service: what kind of answer it takes, how it's worded, whether it blocks
// joining, and — for the two select types — what there is to choose from.
public partial class AddEditIntakeFieldPageViewModel : BaseViewModel
{
    private readonly IIntakeFieldsService _intakeFieldsService;
    private readonly IQueuePopupService _popupService;
    private Guid _serviceId;
    private Guid? _editingFieldId;
    private int _sortOrder;

    public AddEditIntakeFieldPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IIntakeFieldsService intakeFieldsService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _intakeFieldsService = intakeFieldsService;
        _popupService = popupService;

        foreach (var fieldType in IntakeFieldTypes.All)
            TypeOptions.Add(IntakeTypeOption.From(fieldType));

        TypeOptions[0].IsSelected = true;
        SelectedType = IntakeFieldTypes.ShortText;
    }

    public IValidator LabelValidator { get; } = new RequiredValidator("The question is required.");

    public ObservableCollection<IntakeTypeOption> TypeOptions { get; } = new();
    public ObservableCollection<IntakeOptionRow> Options { get; } = new();

    // Earlier single/multi-select questions this one could be conditional on, and — once one is
    // picked — the answers on it that would make this question show up.
    public ObservableCollection<IntakeTriggerFieldOption> TriggerFieldOptions { get; } = new();
    public ObservableCollection<IntakeOptionItem> TriggerValueOptions { get; } = new();

    public string SelectedType { get; set; } = IntakeFieldTypes.ShortText;
    public string Label { get; set; } = "";
    public bool IsRequired { get; set; }
    public string NewOptionText { get; set; } = "";
    public bool HasCondition { get; set; }
    public bool IsSaving { get; set; }
    public bool IsDeleting { get; set; }

    public bool IsEditing => _editingFieldId is not null;
    public bool ShowOptions => IntakeFieldTypes.HasOptions(SelectedType);
    public bool NeedsMoreOptions => ShowOptions && Options.Count < 2;

    // Nothing to be conditional on until an earlier single/multi-select question exists.
    public bool ShowConditionSection => TriggerFieldOptions.Count > 0;
    public bool ShowTriggerValues => TriggerValueOptions.Count > 0;
    public string PageTitle { get; set; } = "Add question";

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _serviceId = parameters is not null && parameters.TryGetValue(NavigationKeys.ServiceId, out var serviceObj)
                ? (Guid)serviceObj
                : throw new InvalidOperationException("AddEditIntakeFieldPage requires a serviceId.");

            var existingFields = await _intakeFieldsService.GetFieldsForServiceAsync(_serviceId);

            if (parameters is not null && parameters.TryGetValue(NavigationKeys.IntakeFieldId, out var fieldObj))
            {
                _editingFieldId = (Guid)fieldObj;
                PageTitle = "Edit question";
                OnPropertyChanged(nameof(IsEditing));
                var field = existingFields.FirstOrDefault(f => f.Id == _editingFieldId);
                BuildTriggerCandidates(existingFields, field?.SortOrder ?? 0, _editingFieldId);
                Apply(field);
                return;
            }

            // A new question goes on the end of whatever is already there.
            _sortOrder = existingFields.Count == 0 ? 0 : existingFields.Max(f => f.SortOrder) + 1;
            BuildTriggerCandidates(existingFields, _sortOrder, null);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // A rule can only point at an earlier single/multi-select question in the same service: earlier
    // so the answer already exists by the time this one would show, and single/multi-select because
    // those are the only types with a fixed set of values to match against.
    private void BuildTriggerCandidates(List<IntakeFieldResponse> existingFields, int currentSortOrder, Guid? excludingId)
    {
        TriggerFieldOptions.Clear();

        var candidates = existingFields
            .Where(f => f.Id != excludingId)
            .Where(f => f.SortOrder < currentSortOrder)
            .Where(f => IntakeFieldTypes.HasOptions(f.FieldType))
            .OrderBy(f => f.SortOrder);

        foreach (var candidate in candidates)
            TriggerFieldOptions.Add(new IntakeTriggerFieldOption { Field = candidate });

        OnPropertyChanged(nameof(ShowConditionSection));
    }

    public void Apply(IntakeFieldResponse? field)
    {
        try
        {
            if (field is null)
                return;

            _sortOrder = field.SortOrder;
            Label = field.Label;
            IsRequired = field.IsRequired;
            SelectType(TypeOptions.FirstOrDefault(t => t.FieldType == field.FieldType));

            Options.Clear();
            foreach (var option in field.Options ?? new List<string>())
                Options.Add(new IntakeOptionRow { Text = option });

            RaiseOptionState();

            HasCondition = field.VisibilityRule is not null;
            if (field.VisibilityRule is { } rule)
            {
                SelectTriggerField(TriggerFieldOptions.FirstOrDefault(t => t.FieldId == rule.FieldId));
                foreach (var value in TriggerValueOptions.Where(o => rule.Values.Contains(o.Text)))
                    value.IsSelected = true;
            }
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void SelectType(IntakeTypeOption? option)
    {
        try
        {
            if (option is null)
                return;

            foreach (var candidate in TypeOptions)
                candidate.IsSelected = ReferenceEquals(candidate, option);

            SelectedType = option.FieldType;
            OnPropertyChanged(nameof(ShowOptions));
            RaiseOptionState();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public void AddOption()
    {
        try
        {
            var text = NewOptionText.Trim();
            if (text.Length == 0 || Options.Any(o => string.Equals(o.Text, text, StringComparison.OrdinalIgnoreCase)))
                return;

            Options.Add(new IntakeOptionRow { Text = text });
            NewOptionText = "";
            RaiseOptionState();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public void RemoveOption(IntakeOptionRow? option)
    {
        try
        {
            if (option is null)
                return;

            Options.Remove(option);
            RaiseOptionState();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public void SelectTriggerField(IntakeTriggerFieldOption? option)
    {
        try
        {
            foreach (var candidate in TriggerFieldOptions)
                candidate.IsSelected = ReferenceEquals(candidate, option);

            TriggerValueOptions.Clear();
            foreach (var value in option?.Field.Options ?? new List<string>())
                TriggerValueOptions.Add(new IntakeOptionItem { Text = value });

            OnPropertyChanged(nameof(ShowTriggerValues));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // Which of the trigger question's answers should reveal this one — always a multi-pick, even
    // when the trigger itself only takes one answer at a time.
    [RelayCommand]
    public void SelectTriggerValue(IntakeOptionItem? option)
    {
        try
        {
            if (option is null)
                return;

            option.IsSelected = !option.IsSelected;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        // The button already forces its bound loading flag true on tap, before this ever runs —
        // every exit, including a validation failure, has to go through the same finally or it
        // stays stuck until the page is left and reopened.
        IsSaving = true;
        try
        {
            if (!LabelValidator.Validate(Label))
                return;

            // A select with one choice isn't a choice; a select with none can't be answered at all.
            if (ShowOptions && Options.Count < 2)
            {
                await _popupService.ShowAlertAsync(
                    "Needs choices",
                    "A select question needs at least two choices for the customer to pick from.");
                return;
            }

            IntakeVisibilityRule? visibilityRule = null;
            if (HasCondition)
            {
                var trigger = TriggerFieldOptions.FirstOrDefault(t => t.IsSelected);
                var triggerValues = TriggerValueOptions.Where(o => o.IsSelected).Select(o => o.Text).ToList();

                if (trigger is null || triggerValues.Count == 0)
                {
                    await _popupService.ShowAlertAsync(
                        "Needs a condition",
                        "Pick which earlier question, and which answer to it, should make this one show up.");
                    return;
                }

                visibilityRule = new IntakeVisibilityRule { FieldId = trigger.FieldId, Values = triggerValues };
            }

            var options = ShowOptions ? Options.Select(o => o.Text).ToList() : null;

            if (_editingFieldId is null)
            {
                await _intakeFieldsService.CreateFieldAsync(new CreateIntakeFieldRequest
                {
                    ServiceId = _serviceId,
                    FieldType = SelectedType,
                    Label = Label.Trim(),
                    IsRequired = IsRequired,
                    SortOrder = _sortOrder,
                    Options = options,
                    VisibilityRule = visibilityRule,
                });
            }
            else
            {
                await _intakeFieldsService.UpdateFieldAsync(_editingFieldId.Value, new UpdateIntakeFieldRequest
                {
                    FieldType = SelectedType,
                    Label = Label.Trim(),
                    IsRequired = IsRequired,
                    Options = options,
                    VisibilityRule = visibilityRule,
                });
            }

            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        // Same reason as SaveAsync: the button has already forced its bound loading flag true, so
        // even "cancelled the confirm" has to pass through the finally that clears it.
        IsDeleting = true;
        try
        {
            if (_editingFieldId is null)
                return;

            // Worth spelling out: the answers are safe, the question is not.
            var confirmed = await _popupService.ShowConfirmAsync(
                "Delete this question?",
                "New customers won't be asked it. Answers already given stay on the visits that gave them.",
                "Delete", "Keep it");

            if (!confirmed)
                return;

            await _intakeFieldsService.DeleteFieldAsync(_editingFieldId.Value);
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsDeleting = false;
        }
    }

    public void RaiseOptionState() => OnPropertyChanged(nameof(NeedsMoreOptions));
}
