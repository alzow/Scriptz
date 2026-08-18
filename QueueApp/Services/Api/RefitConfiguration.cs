using Refit;
using ScriptzApp.Constants;
using ScriptzApp.Services.Api.Auth;
using ScriptzApp.Services.Api.Queue;

namespace ScriptzApp.Services.Api;

public static class RefitConfiguration
{
    public static IServiceCollection ConfigureRefitApi(this IServiceCollection services)
    {
        services.AddTransient<SupabaseAuthHeaderHandler>();
        services.AddTransient<HttpLoggingHandler>();

       services.AddRefitClient<IQueueApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(SupabaseConfig.RestUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(SupabaseConfig.AuthUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        return services;
    }
}
