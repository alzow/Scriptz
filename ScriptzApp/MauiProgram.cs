using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MPowerKit.Navigation;
using MPowerKit.Popups;
using ScriptzApp.Services.Api;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using CommunityToolkit.Mvvm.Messaging;
#if USE_STUBS
using ScriptzApp.Services.Stubs;
#endif

namespace ScriptzApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMPowerKitNavigation(mpowerBuilder =>
            {
                mpowerBuilder.ConfigureServices(services =>
                {
                    // Core infrastructure (always real)
                    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                    services.AddSingleton<ISecureStorageService, SecureStorageService>();
                    services.AddSingleton<IScriptzPopupService, ScriptzPopupService>();

#if USE_STUBS
                    // ── Stub services (no backend required) ───────────────────
                    services.AddSingleton<IAuthService, StubAuthService>();
                    services.AddSingleton<IApiService, StubApiService>();
#else
                    // ── Real Refit services ───────────────────────────────────
                    services.AddSingleton<IAuthService, AuthService>();
                    services.ConfigureRefitApi();
#endif

                    // Auto-register Pages and ViewModels
                    services.RegisterPagesAndViewModels();
                });

                mpowerBuilder.OnAppStart("NavigationPage/LoginPage");
            })
            .UseMPowerKitPopups()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
