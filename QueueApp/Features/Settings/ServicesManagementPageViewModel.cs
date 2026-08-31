using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class ServicesManagementPageViewModel : BaseViewModel
{
    public ObservableCollection<ServiceResponse> ActiveServices { get; } = new();
    public ObservableCollection<ServiceResponse> InactiveServices { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsEmpty => ActiveServices.Count == 0 && InactiveServices.Count == 0 && !IsLoading;
    public bool HasInactive => InactiveServices.Count > 0;
    public bool IsInactiveExpanded { get; set; }
    public string InactiveHeaderText { get; set; } = string.Empty;
    public string InactiveChevron => IsInactiveExpanded ? "ic_chevron_up" : "ic_chevron_down";

    private Guid _businessId;

    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IBusinessService _businessService;

    public ServicesManagementPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IServiceOfferingsService serviceOfferingsService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _serviceOfferingsService = serviceOfferingsService;
        _businessService = businessService;
        Title = "Services";
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            _businessId = await _businessService.GetOwnedBusinessIdAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        if (_businessId != Guid.Empty)
            await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var services = await _serviceOfferingsService.GetServicesAsync(_businessId);

            ActiveServices.Clear();
            InactiveServices.Clear();
            foreach (var service in services.Where(s => s.IsActive))
                ActiveServices.Add(service);
            foreach (var service in services.Where(s => !s.IsActive))
                InactiveServices.Add(service);

            InactiveHeaderText = $"Not offered right now ({InactiveServices.Count})";
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddServiceAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddEditServicePage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task EditServiceAsync(ServiceResponse service)
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddEditServicePage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId, [NavigationKeys.ServiceId] = service.Id });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task ReactivateAsync(ServiceResponse service)
    {
        service.IsToggling = true;
        try
        {
            await _serviceOfferingsService.SetServiceActiveAsync(service.Id, true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            service.IsToggling = false;
        }
    }

    [RelayCommand]
    public void ToggleInactiveExpanded()
    {
        IsInactiveExpanded = !IsInactiveExpanded;
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
}
