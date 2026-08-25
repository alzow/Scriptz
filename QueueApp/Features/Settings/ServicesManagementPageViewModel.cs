using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class ServicesManagementPageViewModel : BaseViewModel
{
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IBusinessService _businessService;
    private Guid _businessId;

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

    public ObservableCollection<ServiceResponse> Services { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsAddingService { get; set; }
    public bool IsEmpty => Services.Count == 0 && !IsLoading;

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
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var services = await _serviceOfferingsService.GetServicesAsync(_businessId);
            Services.Clear();
            foreach (var s in services)
                Services.Add(s);
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
    private async Task AddServiceAsync()
    {
        IsAddingService = true;
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddEditServicePage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsAddingService = false;
        }
    }

    [RelayCommand]
    private async Task EditServiceAsync(ServiceResponse service)
    {
        await NavigationService.NavigateAsync(NavigationPaths.AddEditServicePage,
            new NavigationParameters { [NavigationKeys.BusinessId] = _businessId, [NavigationKeys.ServiceId] = service.Id });
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(ServiceResponse service)
    {
        service.IsToggling = true;
        try
        {
            await _serviceOfferingsService.SetServiceActiveAsync(service.Id, !service.IsActive);
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
    private async Task GoBackAsync()
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
