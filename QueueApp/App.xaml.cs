using QueueApp.Constants;
using QueueApp.Features.Auth;
using QueueApp.Features.QueueSplash;
using QueueApp.Framework.Theming;
using QueueApp.Services.Auth;

namespace QueueApp;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    // Guards against a burst of failed calls each queuing its own trip to the login page.
    private int _returningToLogin;

    public App(IServiceProvider services, IAuthService authService)
    {
        _services = services;

        InitializeComponent();

        // Before the first page is built: the stored choice has to be on Application.UserAppTheme
        // by the time anything resolves an AppThemeBinding, or the app flashes the wrong theme.
        ThemeService.Initialise(this);

        // A token that expires mid-session is renewed in the API pipeline and the user never sees
        // it. This is the case that can't be: the refresh token itself is gone or was rejected, so
        // there is no session left to renew and the only honest thing to do is ask for a sign-in.
        authService.SessionExpired += OnSessionExpired;
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _returningToLogin, 1) == 1)
            return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var currentPage = CurrentPage();

                // Nothing on screen yet, or the splash is still deciding where to go — it sends a
                // dead session to the login page itself, so navigating from here too would race it.
                // Already on login: nothing to do.
                if (currentPage is null or LoginPage or QueueSplashPage)
                    return;

                // Resolved here rather than injected: navigation is only usable once the app's first
                // page is up, which is well after this class is constructed.
                var navigationService = _services.GetService<INavigationService>();
                if (navigationService is null)
                {
                    System.Diagnostics.Debug.WriteLine("[Auth] session expired, but there is no navigation service to leave with");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[Auth] session expired — returning to login");
                await navigationService.NavigateAsync(NavigationPaths.Login);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Auth] could not return to login after session expiry: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _returningToLogin, 0);
            }
        });
    }

    private static Page? CurrentPage()
    {
        var root = Current?.Windows.FirstOrDefault()?.Page;

        return root switch
        {
            NavigationPage navigation => navigation.CurrentPage ?? navigation,
            TabbedPage tabbed => tabbed.CurrentPage ?? tabbed,
            _ => root
        };
    }
}
