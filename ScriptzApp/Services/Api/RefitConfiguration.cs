using Refit;
using ScriptzApp.Constants;
using ScriptzApp.Services.Api.Queue;

namespace ScriptzApp.Services.Api;

public static class RefitConfiguration
{
    public static IServiceCollection ConfigureRefitApi(this IServiceCollection services)
    {
        services.AddTransient<SupabaseAuthHeaderHandler>();

        services.AddRefitClient<IQueueApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(SupabaseConfig.RestUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>();

        return services;
    }
}
