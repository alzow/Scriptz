using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.BusinessSettings.Constants;
using QueueApp.Features.BusinessSettings.Helpers;
using QueueApp.Features.BusinessSettings.Models;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.BusinessSettings.AddEditService;

// One service, down one page: what it is called and costs, how it runs, and what it asks before
// someone joins. The three panels are the three things a service now is.
public partial class AddEditServicePageViewModel : BaseViewModel
{
    private const string NameRequiredError = "Service name is required.";

    public IValidator NameValidator { get; } = new RequiredValidator(NameRequiredError);

    public ServiceDurationEditor Duration { get; }
    public ServiceQuestionsEditor Questions { get; }

    public string Name { get; set; } = "";
    public string PriceRandText { get; set; } = "";
    public bool RequiresCollection { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsSaving { get; set; }
    public bool IsDeactivating { get; set; }
    public string PageTitle { get; set; } = BusinessSettingsConstants.AddServiceTitle;

    // A question hangs off a service id. Rather than telling the owner to save and come back, the
    // questions panel saves for them — see §4 of the spec.
    public bool IsExistingService => _editingServiceId is not null;
    public bool QuestionsTouched { get; set; }

    public string SaveText => QuestionsTouched && !IsExistingService
        ? BusinessSettingsConstants.SaveAndAddQuestionsText
        : BusinessSettingsConstants.SaveText;

    public string DeactivateText => IsActive
        ? BusinessSettingsConstants.DeactivateText
        : BusinessSettingsConstants.ReactivateText;

    private Guid _businessId;
    private Guid? _editingServiceId;
    private bool _loaded;

    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IQueuePopupService _popupService;

    public AddEditServicePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IServiceOfferingsService serviceOfferingsService,
        IIntakeFieldsService intakeFieldsService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _serviceOfferingsService = serviceOfferingsService;
        _popupService = popupService;

        Duration = new ServiceDurationEditor();
        Questions = new ServiceQuestionsEditor(intakeFieldsService);
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var businessObj)
                ? (Guid)businessObj
                : throw new InvalidOperationException("AddEditServicePage requires a businessId.");

            if (parameters is null || !parameters.TryGetValue(NavigationKeys.ServiceId, out var serviceObj))
                return;

            _editingServiceId = (Guid)serviceObj;
            OnPropertyChanged(nameof(IsExistingService));
            OnPropertyChanged(nameof(SaveText));

            await LoadExistingAsync(_editingServiceId.Value);
            _loaded = true;
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    // Coming back from the question editor is the only way the list changes, so it reloads here
    // rather than being handed a result.
    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();

            if (_loaded && _editingServiceId is not null)
                await Questions.LoadAsync(_editingServiceId.Value);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public async Task LoadExistingAsync(Guid serviceId)
    {
        try
        {
            var servicesTask = _serviceOfferingsService.GetServicesAsync(_businessId);
            var questionsTask = Questions.LoadAsync(serviceId);
            await Task.WhenAll(servicesTask, questionsTask);

            var existing = (await servicesTask).FirstOrDefault(s => s.Id == serviceId);
            if (existing is null)
                return;

            PageTitle = existing.Name;
            Name = existing.Name;
            PriceRandText = existing.PriceCents.HasValue
                ? (existing.PriceCents.Value / 100m).ToString("0.##")
                : "";
            RequiresCollection = existing.RequiresCollection;
            IsActive = existing.IsActive;
            Duration.Load(existing.EstMinutes);

            OnPropertyChanged(nameof(DeactivateText));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void SelectDuration(DurationChoice? choice)
    {
        try
        {
            Duration.Select(choice);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        // The button forces its bound loading flag true on tap, before this runs — every exit,
        // a validation failure included, has to pass through the same finally or it stays stuck.
        IsSaving = true;
        try
        {
            if (await PersistAsync())
                await RunNavigationAsync(() => NavigationService.GoBackAsync());
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

    public async Task<bool> PersistAsync()
    {
        try
        {
            if (!NameValidator.Validate(Name))
                return false;

            var minutes = Duration.ResolveMinutes();
            if (minutes is null)
            {
                await _popupService.ShowAlertAsync(
                    BusinessSettingsConstants.DurationInvalidTitle,
                    BusinessSettingsConstants.DurationInvalidMessage);
                return false;
            }

            int? priceCents = null;
            if (!string.IsNullOrWhiteSpace(PriceRandText) && decimal.TryParse(PriceRandText, out var rand))
                priceCents = (int)Math.Round(rand * 100);

            if (_editingServiceId is null)
            {
                var created = await _serviceOfferingsService.CreateServiceAsync(new CreateServiceRequest
                {
                    BusinessId = _businessId,
                    Name = Name.Trim(),
                    EstMinutes = minutes.Value,
                    PriceCents = priceCents,
                    RequiresCollection = RequiresCollection,
                });

                _editingServiceId = created.FirstOrDefault()?.Id;
                _loaded = true;
                PageTitle = Name.Trim();
                OnPropertyChanged(nameof(IsExistingService));
                OnPropertyChanged(nameof(SaveText));
                OnPropertyChanged(nameof(DeactivateText));
                return true;
            }

            await _serviceOfferingsService.UpdateServiceAsync(_editingServiceId.Value, new UpdateServiceRequest
            {
                Name = Name.Trim(),
                EstMinutes = minutes.Value,
                PriceCents = priceCents,
                RequiresCollection = RequiresCollection,
            });

            return true;
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
            return false;
        }
    }

    // The create screen used to say "save this service first" on the one screen where someone is
    // most likely to want a question. It saves for them instead.
    [RelayCommand]
    public async Task AddQuestionAsync()
    {
        try
        {
            QuestionsTouched = true;
            OnPropertyChanged(nameof(SaveText));

            if (_editingServiceId is null && !await PersistAsync())
                return;

            if (_editingServiceId is null)
                return;

            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.AddEditIntakeFieldPage,
                new NavigationParameters { [NavigationKeys.ServiceId] = _editingServiceId.Value }));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task EditQuestionAsync(IntakeQuestionRow? row)
    {
        try
        {
            if (row is null || _editingServiceId is null)
                return;

            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.AddEditIntakeFieldPage,
                new NavigationParameters
                {
                    [NavigationKeys.ServiceId] = _editingServiceId.Value,
                    [NavigationKeys.IntakeFieldId] = row.Id,
                }));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public Task MoveQuestionUpAsync(IntakeQuestionRow? row) => MoveQuestionAsync(row, -1);

    [RelayCommand]
    public Task MoveQuestionDownAsync(IntakeQuestionRow? row) => MoveQuestionAsync(row, 1);

    // A move that would put a question above the answer it depends on is refused, and the refusal
    // says which two questions and why.
    public async Task MoveQuestionAsync(IntakeQuestionRow? row, int direction)
    {
        try
        {
            var refusal = await Questions.MoveAsync(row, direction);

            if (refusal is not null)
                await _popupService.ShowAlertAsync(IntakeQuestionConstants.ReorderBlockedTitle, refusal);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task PreviewFormAsync()
    {
        try
        {
            if (_editingServiceId is null)
                return;

            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.IntakeFormPreviewPage,
                new NavigationParameters { [NavigationKeys.ServiceId] = _editingServiceId.Value }));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task ToggleActiveAsync()
    {
        IsDeactivating = true;
        try
        {
            if (_editingServiceId is null)
                return;

            if (IsActive)
            {
                var confirmed = await _popupService.ShowConfirmAsync(
                    BusinessSettingsConstants.DeactivateConfirmTitle,
                    BusinessSettingsConstants.DeactivateConfirmMessage,
                    BusinessSettingsConstants.DeactivateConfirmAccept,
                    BusinessSettingsConstants.DeactivateConfirmCancel);

                if (!confirmed)
                    return;
            }

            await _serviceOfferingsService.SetServiceActiveAsync(_editingServiceId.Value, !IsActive);
            IsActive = !IsActive;
            OnPropertyChanged(nameof(DeactivateText));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsDeactivating = false;
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.GoBackAsync());
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }
}
