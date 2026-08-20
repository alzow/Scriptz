using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class BusinessSettingsPageViewModel : BaseViewModel
{
    public BusinessSettingsPageViewModel(INavigationService navigationService, ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
        Title = "Business Settings";
    }

    [RelayCommand]
    private async Task OpenServicesAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.ServicesManagementPage);
    }

    [RelayCommand]
    private async Task OpenStaffAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.StaffManagementPage);
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
