using Refit;
using QueueApp.Constants;
using QueueApp.Services.Api.Auth;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;

namespace QueueApp.Services.Api;

public static class RefitConfiguration
{
    public const string SupabaseStorageClientName = "SupabaseStorage";

    public static IServiceCollection ConfigureRefitApi(this IServiceCollection services)
    {
        services.AddTransient<SupabaseAuthHeaderHandler>();
        services.AddTransient<SupabaseAnonKeyHandler>();
        services.AddTransient<HttpLoggingHandler>();

        services.AddApiClient<IQueueApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IBusinessApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IOperatorApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IAuthApi>(SupabaseConfig.AuthUrl);
        services.AddApiClient<IDeviceTokenApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IProfileApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IServiceOfferingsApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IBookingApi>(SupabaseConfig.RestUrl);
        services.AddApiClient<IIntakeFieldsApi>(SupabaseConfig.RestUrl);
        services.AddHttpClient(SupabaseStorageClientName, client => client.BaseAddress = new Uri(SupabaseConfig.ProjectUrl))
            .AddHttpMessageHandler<SupabaseAuthHeaderHandler>();

        // The one client that must not go through SupabaseAuthHeaderHandler: that handler renews the
        // token by calling this, so sending this through it would be a cycle. See ITokenRefreshApi.
        services.AddApiClient<ITokenRefreshApi, SupabaseAnonKeyHandler>(SupabaseConfig.AuthUrl);

        return services;
    }

    private static void AddApiClient<T>(this IServiceCollection services, string baseUrl)
        where T : class
        => services.AddApiClient<T, SupabaseAuthHeaderHandler>(baseUrl);

    // HttpLoggingHandler reads every request and response body into a string and writes the lot to
    // logcat, on the calling thread, before the deserialiser ever sees it. That is worth paying while
    // debugging and worth nothing in a release build, so it is only in the pipeline for one of them.
    private static void AddApiClient<TApi, THandler>(this IServiceCollection services, string baseUrl)
        where TApi : class
        where THandler : DelegatingHandler
    {
        var builder = services.AddRefitClient<TApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<THandler>();

#if DEBUG
        builder.AddHttpMessageHandler<HttpLoggingHandler>();
#endif
    }
}
