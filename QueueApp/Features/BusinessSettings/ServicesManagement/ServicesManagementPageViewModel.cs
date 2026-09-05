using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.BusinessSettings.Constants;
using QueueApp.Features.BusinessSettings.Models;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BusinessSettings.ServicesManagement;

public partial class ServicesManagementPageViewModel : BaseViewModel
{
    public ObservableCollection<ServiceRow> Services { get; } = new();
    public ObservableCollection<ServiceRow> InactiveServices { get; } = new();

    public bool IsLoading { get; set; }
    public bool IsInactiveExpanded { get; set; }

    public bool IsEmpty => Services.Count == 0 && InactiveServices.Count == 0 && !IsLoading;
    public bool HasInactive => InactiveServices.Count > 0;
    public string InactiveCountText => InactiveServices.Count.ToString();

    private Guid _businessId;

    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IIntakeFieldsService _intakeFieldsService;
    private readonly IBusinessService _businessService;

    public ServicesManagementPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IServiceOfferingsService serviceOfferingsService,
        IIntakeFieldsService intakeFieldsService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _serviceOfferingsService = serviceOfferingsService;
        _intakeFieldsService = intakeFieldsService;
        _businessService = businessService;
        Title = BusinessSettingsConstants.ServicesTitle;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            _businessId = await _businessService.GetOwnedBusinessIdAsync();
            await LoadAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();
            if (_businessId != Guid.Empty)
                await LoadAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    // The question counts are one read for the whole business, started alongside the services
    // themselves — a row that says "2 QUESTIONS" must not cost a round trip per service.
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var servicesTask = _serviceOfferingsService.GetServicesAsync(_businessId);
            var questionsTask = _intakeFieldsService.GetFieldsByServiceAsync(_businessId);
            await Task.WhenAll(servicesTask, questionsTask);

            var services = await servicesTask;
            var questions = await questionsTask;

            Services.Clear();
            InactiveServices.Clear();

            foreach (var service in services)
            {
                var count = questions.TryGetValue(service.Id, out var fields) ? fields.Count : 0;
                var row = ServiceRow.From(service, count);

                if (service.IsActive)
                    Services.Add(row);
                else
                    InactiveServices.Add(row);
            }

            RaiseListState();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsLoading = false;
            RaiseListState();
        }
    }

    [RelayCommand]
    public async Task AddServiceAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.AddEditServicePage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId }));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task EditServiceAsync(ServiceRow? service)
    {
        try
        {
            if (service is null)
                return;

            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.AddEditServicePage,
                new NavigationParameters
                {
                    [NavigationKeys.BusinessId] = _businessId,
                    [NavigationKeys.ServiceId] = service.Id,
                }));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void ToggleInactive()
    {
        try
        {
            IsInactiveExpanded = !IsInactiveExpanded;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
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

    // Fody cannot see through an ObservableCollection, so what is derived from one has to say so.
    public void RaiseListState()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasInactive));
        OnPropertyChanged(nameof(InactiveCountText));
    }
}
