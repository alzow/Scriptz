# STEP 6: Create Navigation Setup and Extensions

This step creates the navigation startup logic and service registration helpers.

## Create NavigationStartup.cs (in root project folder)

```csharp
using MPowerKit.Navigation;
using ScriptzApp.Features.Auth;
using System.Reflection;

namespace ScriptzApp;

public static class NavigationStartup
{
    public static void Configure(INavigationService navigationService)
    {
        // Set initial navigation stack - starts at Login
        navigationService.NavigateAsync("NavigationPage/LoginPage");
    }

    public static IServiceCollection RegisterPagesAndViewModels(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Register all Pages
        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Page)) && !t.IsAbstract);

        foreach (var pageType in pageTypes)
        {
            services.AddTransient(pageType);

            // Auto-register corresponding ViewModel
            var viewModelName = $"{pageType.FullName}ViewModel";
            var viewModelType = assembly.GetType(viewModelName);

            if (viewModelType != null)
            {
                services.AddTransient(viewModelType);
            }
            else
            {
                // Try alternative naming (remove "Page" and add "ViewModel")
                viewModelName = $"{pageType.FullName.Replace("Page", "")}ViewModel";
                viewModelType = assembly.GetType(viewModelName);
                
                if (viewModelType != null)
                {
                    services.AddTransient(viewModelType);
                }
            }
        }

        return services;
    }
}
```

## Create Framework/Extensions/ServiceCollectionExtensions.cs

```csharp
namespace ScriptzApp.Framework.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScriptzServices(this IServiceCollection services)
    {
        // This is a helper extension method for organizing service registrations
        // All service registrations are done in MauiProgram.cs
        return services;
    }
}
```

**STOP HERE - Confirm navigation files are created before proceeding to Step 7**
