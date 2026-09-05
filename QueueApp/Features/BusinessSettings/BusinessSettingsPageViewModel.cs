using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BusinessSettings;

public partial class BusinessSettingsPageViewModel : BaseViewModel
{
    public BusinessSettingsPageViewModel(INavigationService navigationService, ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
        Title = "Business Settings";
    }

    [RelayCommand]
    public async Task OpenServicesAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.ServicesManagementPage));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task OpenStaffAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.StaffManagementPage));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task OpenHoursAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.OperatorHoursPage));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task OpenLocationAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.NavigateAsync(NavigationPaths.BusinessLocationPage));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public override bool TryHandleSystemBack()
    {
        GoBackCommand.Execute(null);
        return true;
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.GoBackAsync());
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
