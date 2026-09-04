using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Auth;
using QueueApp.Services.Onboarding;
using QueueApp.Services.Storage;

namespace QueueApp.Features.QueueSplash;

public class QueueSplashPageViewModel : BaseViewModel
{
    public bool BypassWelcome { get; set; }

    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IBusinessService _businessService;
    private readonly IFirstRunService _firstRunService;

    public QueueSplashPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IBusinessService businessService,
        IFirstRunService firstRunService)
        : base(navigationService, secureStorageService)
    {
        _navigationService = navigationService;
        _authService = authService;
        _businessService = businessService;
        _firstRunService = firstRunService;
    }

    public override void Initialize(INavigationParameters parameters)
    {
        base.Initialize(parameters);

        BypassWelcome = parameters is not null
            && parameters.TryGetValue(NavigationKeys.BypassWelcome, out var bypass)
            && bypass is true;
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        await SplashOrchestration();
    }

    public async Task SplashOrchestration()
    {
        try
        {
            var isValid = await _authService.EnsureValidSessionAsync();

            if (!isValid)
            {
                await _navigationService.NavigateAsync(SignedOutDestination());
                return;
            }

            // Someone with a live session is past the pitch whether or not this install ever showed
            // it — a reinstall that restores a session must not open on the welcome screen.
            _firstRunService.MarkWelcomeSeen();

            var (ownsBusiness, mode) = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
            var uri = MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: ownsBusiness, manageMode: mode);
            await _navigationService.NavigateAsync(uri);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            await _navigationService.NavigateAsync($"/{NavigationPaths.Login}");
        }
    }

    // The welcome screen is for someone who has never been in the app. Everyone else — a customer
    // who signed out, a returning install, anyone arriving through a link — gets sign-in.
    public string SignedOutDestination() =>
        BypassWelcome || _firstRunService.HasSeenWelcome
            ? $"/{NavigationPaths.Login}"
            : $"/{NavigationPaths.Welcome}";
}
