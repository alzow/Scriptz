using Refit;
using QueueApp.Constants;
using QueueApp.Services.Api.Auth;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;

namespace QueueApp.Services.Api;

public static class RefitConfiguration
{
    public static IServiceCollection ConfigureRefitApi(this IServiceCollection services)
    {
        services.AddTransient<SupabaseAuthHeaderHandler>();
        services.AddTransient<HttpLoggingHandler>();

        services.AddApiClient<IQueueApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IBusinessApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IOperatorApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IAuthApi>(SupabaseConfig.AuthUrl);
        services.AddApiClient<IProfileApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IServiceOfferingsApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IBookingApi>(SupabaseConfig.RestUrl);

        return services;
    }

    // HttpLoggingHandler reads every request and response body into a string and writes the lot to
    // logcat, on the calling thread, before the deserialiser ever sees it. That is worth paying while
    // debugging and worth nothing in a release build, so it is only in the pipeline for one of them.
    private static void AddApiClient<T>(this IServiceCollection services, string baseUrl)
        where T : class
    {
        var builder = services.AddRefitClient<T>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>();

#if DEBUG
        builder.AddHttpMessageHandler<HttpLoggingHandler>();
#endif
    }
}
