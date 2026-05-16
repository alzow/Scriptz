using System.Reflection;

namespace ScriptzApp;

public static class NavigationStartup
{
    public static IServiceCollection RegisterPagesAndViewModels(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Page)) && !t.IsAbstract);

        foreach (var pageType in pageTypes)
        {
            services.AddTransient(pageType);

            var viewModelName = $"{pageType.FullName}ViewModel";
            var viewModelType = assembly.GetType(viewModelName);

            if (viewModelType != null)
            {
                services.AddTransient(viewModelType);
            }
            else
            {
                viewModelName = $"{pageType.FullName!.Replace("Page", "")}ViewModel";
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
