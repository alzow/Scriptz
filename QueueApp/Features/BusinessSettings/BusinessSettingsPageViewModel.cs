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
        await NavigationService.NavigateAsync(NavigationPaths.ServicesManagementPage);
    }

    [RelayCommand]
    public async Task OpenStaffAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.StaffManagementPage);
    }

    [RelayCommand]
    public async Task OpenHoursAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.OperatorHoursPage);
    }

    [RelayCommand]
    public async Task OpenLocationAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.BusinessLocationPage);
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
