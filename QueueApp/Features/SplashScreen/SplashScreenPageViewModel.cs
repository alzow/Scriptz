using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;

namespace QueueApp.Features.SplashScreen;

public class SplashScreenPageViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IQueueService _queueService;

    public SplashScreenPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IQueueService queueService)
        : base(navigationService, secureStorageService)
    {
        _navigationService = navigationService;
        _authService = authService;
        _queueService = queueService;
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

            var ownsBusiness = await TryGetOwnedBusinessAsync();
            var uri = BuildMainTabbedUri(includeManageTab: ownsBusiness);
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

    private async Task<bool> TryGetOwnedBusinessAsync()
    {
        try
        {
            var businessId = await _queueService.GetOwnedBusinessIdAsync();
            return businessId != Guid.Empty;
        }
        catch
        {
            return false; // no owned business, or the lookup failed — either way, no Manage tab
        }
    }

    private static string BuildMainTabbedUri(bool includeManageTab)
    {
        var uri = $"/{NavigationPaths.MainTabbedPage}" +
                  $"?{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.CategoryPickerPage}" +
                  $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.HistoryPage}" +
                  $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.ProfilePage}";

        if (includeManageTab)
            uri += $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.OperatorQueuePage}";

        uri += $"&{KnownNavigationParameters.SelectTab}={NavigationPaths.CategoryPickerPage}";

        return uri;
    }
}
