using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.BusinessSettings.Constants;
using QueueApp.Features.BusinessSettings.Helpers;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;
using QueueApp.Shared.Domain.Models;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.BusinessSettings.AddEditIntakeField;

// One question on one service: what it asks, the example under it, what kind of answer it takes,
// whether it blocks joining, and when it is asked at all.
public partial class AddEditIntakeFieldPageViewModel : BaseViewModel
{
    public IValidator LabelValidator { get; } = new RequiredValidator(IntakeQuestionConstants.PromptRequiredError);

    public ObservableCollection<IntakeTypeOption> TypeOptions { get; } = new();
    public ObservableCollection<IntakeOptionRow> Options { get; } = new();

    public IntakeRuleEditor Rule { get; } = new();

    public string SelectedType { get; set; } = IntakeFieldTypes.ShortText;
    public string Label { get; set; } = "";
    public string Hint { get; set; } = "";
    public bool IsRequired { get; set; }
    public string NewOptionText { get; set; } = "";

    public bool IsSaving { get; set; }
    public bool IsDeleting { get; set; }

    public bool IsEditing => _editingFieldId is not null;
    public bool ShowOptions => IntakeFieldTypes.HasOptions(SelectedType);
    public bool NeedsMoreOptions => ShowOptions && Options.Count < 2;

    // Files are the one kind with a consequence outside the app — see §5 of the spec and the
    // retention rules in Documentation/service-intake-fields-backend-requirements.md.
    public bool ShowFileWarning => SelectedType == IntakeFieldTypes.File;

    public string PageTitle { get; set; } = IntakeQuestionConstants.AddTitle;

    private Guid _serviceId;
    private Guid? _editingFieldId;
    private int _sortOrder;
    private List<IntakeFieldResponse> _existingFields = new();

    private readonly IIntakeFieldsService _intakeFieldsService;
    private readonly IQueuePopupService _popupService;

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

        SelectType(TypeOptions[0]);
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _serviceId = parameters is not null && parameters.TryGetValue(NavigationKeys.ServiceId, out var serviceObj)
                ? (Guid)serviceObj
                : throw new InvalidOperationException("AddEditIntakeFieldPage requires a serviceId.");

            _existingFields = (await _intakeFieldsService.GetFieldsForServiceAsync(_serviceId))
                .OrderBy(f => f.SortOrder)
                .ToList();

            if (parameters is not null && parameters.TryGetValue(NavigationKeys.IntakeFieldId, out var fieldObj))
            {
                _editingFieldId = (Guid)fieldObj;
                PageTitle = IntakeQuestionConstants.EditTitle;
                OnPropertyChanged(nameof(IsEditing));

                var field = _existingFields.FirstOrDefault(f => f.Id == _editingFieldId);
                Rule.Build(_existingFields, field?.SortOrder ?? 0, _editingFieldId);
                Apply(field);
                return;
            }

            // A new question goes on the end of whatever is already there.
            _sortOrder = _existingFields.Count == 0 ? 0 : _existingFields.Max(f => f.SortOrder) + 1;
            Rule.Build(_existingFields, _sortOrder, null);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public void Apply(IntakeFieldResponse? field)
    {
        try
        {
            if (field is null)
                return;

            _sortOrder = field.SortOrder;
            Label = field.Label;
            Hint = field.Hint ?? "";
            IsRequired = field.IsRequired;
            SelectType(TypeOptions.FirstOrDefault(t => t.FieldType == field.FieldType));

            Options.Clear();
            foreach (var option in field.Options ?? new List<string>())
                Options.Add(new IntakeOptionRow { Text = option });

            RaiseOptionState();
            Rule.Load(field.VisibilityRule);
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
            OnPropertyChanged(nameof(ShowFileWarning));
            RaiseOptionState();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void ChooseAlways() => SetConditional(false);

    [RelayCommand]
    public void ChooseConditional() => SetConditional(true);

    public void SetConditional(bool conditional)
    {
        try
        {
            Rule.SetConditional(conditional);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task PickRuleQuestionAsync()
    {
        try
        {
            var labels = Rule.QuestionLabels;
            if (labels.Length == 0)
                return;

            var chosen = await _popupService.ShowActionSheetAsync(
                IntakeQuestionConstants.RuleLead, IntakeQuestionConstants.DeleteConfirmCancel, labels);

            if (chosen is not null)
                Rule.SelectQuestion(chosen);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task PickRuleValueAsync()
    {
        try
        {
            var values = Rule.ValueOptions;
            if (values.Length == 0)
                return;

            var chosen = await _popupService.ShowActionSheetAsync(
                Rule.SelectedQuestionLabel, IntakeQuestionConstants.DeleteConfirmCancel, values);

            if (chosen is not null)
                Rule.SelectValue(chosen);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // Removing an option silently is how a conditional question quietly starts asking everyone for
    // a medical aid card. The warning names what breaks before it happens.
    [RelayCommand]
    public async Task RemoveOptionAsync(IntakeOptionRow? option)
    {
        try
        {
            if (option is null)
                return;

            if (!await ConfirmOptionRemovalAsync(option.Text))
                return;

            Options.Remove(option);
            RaiseOptionState();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public async Task<bool> ConfirmOptionRemovalAsync(string option)
    {
        if (_editingFieldId is not { } fieldId)
            return true;

        var dependants = IntakeQuestionHelper.DependantsOnOption(fieldId, option, _existingFields);
        if (dependants.Count == 0)
            return true;

        return await _popupService.ShowConfirmAsync(
            IntakeQuestionConstants.OptionDependantsTitle,
            IntakeQuestionHelper.OptionRemovalWarning(option, dependants),
            IntakeQuestionConstants.OptionDependantsAccept,
            IntakeQuestionConstants.OptionDependantsCancel);
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        // The button forces its bound loading flag true on tap, before this runs — every exit, a
        // validation failure included, has to pass through the same finally or it stays stuck.
        IsSaving = true;
        try
        {
            if (!LabelValidator.Validate(Label))
                return;

            // A select with one choice isn't a choice; a select with none can't be answered at all.
            if (ShowOptions && Options.Count < 2)
            {
                await _popupService.ShowAlertAsync(
                    IntakeQuestionConstants.OptionsNeededTitle,
                    IntakeQuestionConstants.OptionsNeededMessage);
                return;
            }

            if (Rule.IsIncomplete)
            {
                await _popupService.ShowAlertAsync(
                    IntakeQuestionConstants.ConditionNeededTitle,
                    IntakeQuestionConstants.ConditionNeededMessage);
                return;
            }

            await PersistAsync();
            await NavigationService.GoBackAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsSaving = false;
        }
    }

    public Task PersistAsync()
    {
        var options = ShowOptions ? Options.Select(o => o.Text).ToList() : null;
        var hint = string.IsNullOrWhiteSpace(Hint) ? null : Hint.Trim();

        if (_editingFieldId is null)
        {
            return _intakeFieldsService.CreateFieldAsync(new CreateIntakeFieldRequest
            {
                ServiceId = _serviceId,
                FieldType = SelectedType,
                Label = Label.Trim(),
                Hint = hint,
                IsRequired = IsRequired,
                SortOrder = _sortOrder,
                Options = options,
                VisibilityRule = Rule.ToRule(),
            });
        }

        return _intakeFieldsService.UpdateFieldAsync(_editingFieldId.Value, new UpdateIntakeFieldRequest
        {
            FieldType = SelectedType,
            Label = Label.Trim(),
            Hint = hint,
            IsRequired = IsRequired,
            Options = options,
            VisibilityRule = Rule.ToRule(),
        });
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

            if (!await ConfirmDeleteAsync(_editingFieldId.Value))
                return;

            await _intakeFieldsService.DeleteFieldAsync(_editingFieldId.Value);
            await NavigationService.GoBackAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsDeleting = false;
        }
    }

    // A question with dependants cannot be deleted without naming them: the questions that pointed
    // at it do not disappear, they start being asked of everyone.
    public Task<bool> ConfirmDeleteAsync(Guid fieldId)
    {
        var dependants = IntakeQuestionHelper.Dependants(fieldId, _existingFields);

        if (dependants.Count == 0)
        {
            return _popupService.ShowConfirmAsync(
                IntakeQuestionConstants.DeleteConfirmTitle,
                IntakeQuestionConstants.DeleteConfirmMessage,
                IntakeQuestionConstants.DeleteConfirmAccept,
                IntakeQuestionConstants.DeleteConfirmCancel);
        }

        return _popupService.ShowConfirmAsync(
            IntakeQuestionConstants.DeleteDependantsTitle,
            IntakeQuestionHelper.DeleteWarning(dependants),
            IntakeQuestionConstants.DeleteDependantsAccept,
            IntakeQuestionConstants.DeleteConfirmCancel);
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public void RaiseOptionState() => OnPropertyChanged(nameof(NeedsMoreOptions));
}
