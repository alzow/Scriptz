using Refit;
using ScriptzApp.Services.Auth;
using System.Net.Http.Headers;

namespace ScriptzApp.Services.Api;

public static class RefitConfiguration
{
    // TODO: Update this with your actual API URL
    private const string BaseUrl = "https://your-api-url.com";

    public static IServiceCollection ConfigureRefitApi(this IServiceCollection services)
    {
        services.AddRefitClient<IScriptzApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                c.BaseAddress = new Uri(BaseUrl);
            })
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddTransient<AuthenticationHandler>();
        services.AddSingleton<IApiService, ApiService>();

        return services;
    }
}

public class AuthenticationHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthenticationHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
