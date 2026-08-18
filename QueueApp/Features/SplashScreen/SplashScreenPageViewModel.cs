using QueueApp.Constants;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;

namespace QueueApp.Features.SplashScreen;

public class SplashScreenPageViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;

    public SplashScreenPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService)
        : base(navigationService, secureStorageService)
    {
        _navigationService = navigationService;
        _authService = authService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            var isValid = await _authService.EnsureValidSessionAsync();
            var destination = isValid ? NavigationPaths.OperatorQueuePage : NavigationPaths.LoginPage;

            await _navigationService.NavigateAsync($"/NavigationPage/{destination}");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            await _navigationService.NavigateAsync($"/NavigationPage/{NavigationPaths.LoginPage}");
        }
    }
}
