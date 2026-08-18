using Refit;
using QueueApp.Constants;
using QueueApp.Services.Api.Auth;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;

namespace QueueApp.Services.Api;

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

        services.AddRefitClient<IBusinessApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(SupabaseConfig.RestUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        services.AddRefitClient<IOperatorApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(SupabaseConfig.RestUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(SupabaseConfig.AuthUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        services.AddRefitClient<IProfileApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(SupabaseConfig.RestUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        return services;
    }
}
