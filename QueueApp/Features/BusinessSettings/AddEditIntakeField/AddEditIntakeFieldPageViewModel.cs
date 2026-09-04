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

    public string SelectedType { get; set; } = IntakeFieldTypes.ShortText;
    public string Label { get; set; } = "";
    public bool IsRequired { get; set; }
    public string NewOptionText { get; set; } = "";
    public bool IsSaving { get; set; }
    public bool IsDeleting { get; set; }

    public bool IsEditing => _editingFieldId is not null;
    public bool ShowOptions => IntakeFieldTypes.HasOptions(SelectedType);
    public bool NeedsMoreOptions => ShowOptions && Options.Count < 2;
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
                Apply(existingFields.FirstOrDefault(f => f.Id == _editingFieldId));
                return;
            }

            // A new question goes on the end of whatever is already there.
            _sortOrder = existingFields.Count == 0 ? 0 : existingFields.Max(f => f.SortOrder) + 1;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void Apply(IntakeFieldResponse? field)
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

        IsSaving = true;
        try
        {
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
        if (_editingFieldId is null)
            return;

        // Worth spelling out: the answers are safe, the question is not.
        var confirmed = await _popupService.ShowConfirmAsync(
            "Delete this question?",
            "New customers won't be asked it. Answers already given stay on the visits that gave them.",
            "Delete", "Keep it");

        if (!confirmed)
            return;

        IsDeleting = true;
        try
        {
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
