using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Auth;
using QueueApp.Services.Notifications;
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
    private readonly IPushNotificationRouter _pushNotificationRouter;

    public QueueSplashPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IBusinessService businessService,
        IFirstRunService firstRunService,
        IPushNotificationRouter pushNotificationRouter)
        : base(navigationService, secureStorageService)
    {
        _navigationService = navigationService;
        _authService = authService;
        _businessService = businessService;
        _firstRunService = firstRunService;
        _pushNotificationRouter = pushNotificationRouter;
    }

    public override void Initialize(INavigationParameters parameters)
    {
        try
        {
            base.Initialize(parameters);

            BypassWelcome = parameters is not null
                && parameters.TryGetValue(NavigationKeys.BypassWelcome, out var bypass)
                && bypass is true;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();
            await SplashOrchestration();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
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

            // The tabs are the first thing a routed notification can be shown over, so a tap held
            // since launch is replayed here rather than the moment it arrived.
            _pushNotificationRouter.NotifyTabsReady();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            await _navigationService.NavigateAsync($"/{NavigationPaths.Login}");
        }
    }

    // The welcome screen is for someone who has never been in the app. Everyone else — a customer
    // who signed out, a returning install, anyone who tapped a notification — gets sign-in, because
    // someone who tapped a notice about their own visit did not ask for the pitch.
    public string SignedOutDestination() =>
        BypassWelcome || _pushNotificationRouter.HasPendingTap || _firstRunService.HasSeenWelcome
            ? $"/{NavigationPaths.Login}"
            : $"/{NavigationPaths.Welcome}";
}
