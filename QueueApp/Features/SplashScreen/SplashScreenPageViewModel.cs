using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;

namespace QueueApp.Features.SplashScreen;

public class SplashScreenPageViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IBusinessService _businessService;

    public SplashScreenPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _navigationService = navigationService;
        _authService = authService;
        _businessService = businessService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            var isValid = await _authService.EnsureValidSessionAsync();

            if (!isValid)
            {
                await _navigationService.NavigateAsync($"/{NavigationPaths.LoginPage}");
                return;
            }

            var ownsBusiness = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
            var uri = MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: ownsBusiness);
            await _navigationService.NavigateAsync(uri);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            // On an unexpected failure, fail safe to the login gate rather than guessing at a
            // signed-in shell we can't actually verify.
            await _navigationService.NavigateAsync($"/{NavigationPaths.LoginPage}");
        }
    }

}
