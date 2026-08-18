using System.Reflection;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Utilities;
using QueueApp.Constants;
using QueueApp.Services.Api;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
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

        var pageTypes = assembly
            .DefinedTypes
            .Where(t => t.IsSubclassOf(typeof(Page)) && !t.IsAbstract)
            .ToList();

        foreach (var pageType in pageTypes)
        {
            // Try exact match first (PageViewModel), then strip "Page" suffix (ViewModel)
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
    }

    private static void RegisterApiServices(IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
        services.ConfigureRefitApi();

#if USE_STUBS
        services.AddSingleton<IQueueService, StubQueueService>();
        services.AddSingleton<IBusinessService, StubBusinessService>();
        services.AddSingleton<IOperatorService, StubOperatorService>();
        services.AddSingleton<IQueueRealtimeService, StubQueueRealtimeService>();
        services.AddSingleton<IProfileService, StubProfileService>();
#else
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<IBusinessService, BusinessService>();
        services.AddSingleton<IOperatorService, OperatorService>();
        services.AddSingleton<IQueueRealtimeService, QueueRealtimeService>();
        services.AddSingleton<IProfileService, ProfileService>();
#endif
    }
}
