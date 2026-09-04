using System.Reflection;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Utilities;
using QueueApp.Constants;
using QueueApp.Features.Flow.Helpers;
using QueueApp.Services.Api;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Location;
using QueueApp.Services.Notifications;
using QueueApp.Services.Onboarding;
using QueueApp.Services.Accessibility;
using CommunityToolkit.Mvvm.Messaging;
#if USE_STUBS
using QueueApp.Services.Stubs;
#endif

namespace QueueApp;

internal static class NavigationStartup
{
    public static void Configure(MPowerKitMvvmBuilder builder)
    {
        builder.ConfigureServices(RegisterServices)
               .OnAppStart(NavigationPaths.AppStart);
    }

    private static void RegisterServices(IServiceCollection services)
    {
        RegisterPages(services);
        RegisterHelperServices(services);
        RegisterApiServices(services);
    }

    private static void RegisterPages(IServiceCollection services)
    {
        var assembly = typeof(App).GetTypeInfo().Assembly;

        // PopupPage derives from ContentPage but is never a navigation target: the sheets are shown
        // through IPopupService over whatever page is already up, and registering them here would
        // put them in the navigation registry under names nothing can navigate to.
        var pageTypes = assembly
            .DefinedTypes
            .Where(t => t.IsSubclassOf(typeof(Page)) && !t.IsAbstract)
            .Where(t => !t.IsSubclassOf(typeof(MPowerKit.Popups.PopupPage)))
            .ToList();

        foreach (var pageType in pageTypes)
        {
            var viewModelType =
                assembly.GetType($"{pageType.FullName}ViewModel") ??
                assembly.GetType($"{pageType.FullName!.Replace("Page", "")}ViewModel");

            services.RegisterForNavigation(pageType.AsType(), viewModelType, pageType.Name);
        }
    }

    private static void RegisterHelperServices(IServiceCollection services)
    {
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<IQueuePopupService, QueuePopupService>();

        // Always the real implementation, even in USE_STUBS builds — device GPS/geocoding are OS
        // capabilities, not a Supabase dependency, and it already fails soft (returns null) when
        // permission is denied or no fix is available, so it doesn't need a stub.
        services.AddSingleton<ILocationService, LocationService>();

        // Both are OS/device capabilities rather than Supabase ones, so like ILocationService they
        // stay real in USE_STUBS builds: the permission check reads the phone, and the preferences
        // live in Preferences.Default.
        services.AddSingleton<INotificationPermissionService, NotificationPermissionService>();
        services.AddSingleton<INotificationPreferencesService, NotificationPreferencesService>();

        // Both read the device rather than Supabase — the welcome flag lives in Preferences.Default
        // and the motion preference is an OS accessibility setting — so they stay real in
        // USE_STUBS builds for the same reason location and notifications do.
        services.AddSingleton<IFirstRunService, FirstRunService>();
        services.AddSingleton<IMotionPreferenceService, MotionPreferenceService>();

        // The picker half of this is the device's file picker, not Supabase, so it stays real in
        // USE_STUBS builds for the same reason location does. The upload half is stubbed in the
        // implementation itself, and stays stubbed in every build until the bucket exists.
        services.AddSingleton<IIntakeFileService, IntakeFileService>();

        // The flow's whole dependency surface behind one type, so the queue and booking view models
        // take three constructor parameters instead of thirteen. Transient because the flow builds
        // per-instance caches off it.
        services.AddTransient<FlowServices>();
    }

    private static void RegisterApiServices(IServiceCollection services)
    {
        // Registered before IAuthService because everything that needs a token — the API pipeline
        // and IAuthService alike — goes through it, and it is the only thing that renews one.
        services.AddSingleton<ISessionRefreshService, SessionRefreshService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.ConfigureRefitApi();

#if USE_STUBS
        services.AddSingleton<IQueueService, StubQueueService>();
        services.AddSingleton<IBusinessService, StubBusinessService>();
        services.AddSingleton<IOperatorService, StubOperatorService>();
        services.AddSingleton<IQueueRealtimeService, StubQueueRealtimeService>();
        services.AddSingleton<IProfileService, StubProfileService>();
        services.AddSingleton<IServiceOfferingsService, StubServiceOfferingsService>();
        services.AddSingleton<IBookingService, StubBookingService>();
        services.AddSingleton<IIntakeFieldsService, StubIntakeFieldsService>();
#else
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<IBusinessService, BusinessService>();
        services.AddSingleton<IOperatorService, OperatorService>();
        services.AddSingleton<IQueueRealtimeService, QueueRealtimeService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IServiceOfferingsService, ServiceOfferingsService>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<IIntakeFieldsService, IntakeFieldsService>();
#endif
    }
}
