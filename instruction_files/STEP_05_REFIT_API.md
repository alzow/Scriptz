# STEP 5: Create Refit API Configuration

This step creates the Refit API interface and authentication handler.

## Create Services/Api/IScriptzApi.cs

```csharp
using Refit;
using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Models.Api.Responses;

namespace ScriptzApp.Services.Api;

public interface IScriptzApi
{
    // Auth Endpoints
    [Post("/api/auth/login")]
    Task<AuthResponse> LoginAsync([Body] LoginRequest request);

    [Post("/api/auth/register")]
    Task<AuthResponse> RegisterAsync([Body] RegisterRequest request);

    [Post("/api/auth/refresh")]
    Task<AuthResponse> RefreshTokenAsync([Body] RefreshTokenRequest request);

    // Medications Endpoints
    [Get("/api/medications")]
    Task<List<MedicationResponse>> GetMedicationsAsync();

    [Get("/api/medications/{id}")]
    Task<MedicationResponse> GetMedicationByIdAsync(string id);

    [Post("/api/medications")]
    Task<MedicationResponse> CreateMedicationAsync([Body] CreateMedicationRequest request);

    [Put("/api/medications/{id}")]
    Task<MedicationResponse> UpdateMedicationAsync(string id, [Body] UpdateMedicationRequest request);

    [Delete("/api/medications/{id}")]
    Task DeleteMedicationAsync(string id);

    // Prescriptions Endpoints
    [Get("/api/prescriptions")]
    Task<List<PrescriptionResponse>> GetPrescriptionsAsync();

    [Get("/api/prescriptions/{id}")]
    Task<PrescriptionResponse> GetPrescriptionByIdAsync(string id);

    [Post("/api/prescriptions")]
    Task<PrescriptionResponse> CreatePrescriptionAsync([Body] CreatePrescriptionRequest request);

    [Put("/api/prescriptions/{id}")]
    Task<PrescriptionResponse> UpdatePrescriptionAsync(string id, [Body] UpdatePrescriptionRequest request);

    [Delete("/api/prescriptions/{id}")]
    Task DeletePrescriptionAsync(string id);

    // Reminders Endpoints
    [Get("/api/reminders")]
    Task<List<ReminderResponse>> GetRemindersAsync();

    [Get("/api/reminders/medication/{medicationId}")]
    Task<List<ReminderResponse>> GetRemindersByMedicationAsync(string medicationId);

    [Post("/api/reminders")]
    Task<ReminderResponse> CreateReminderAsync([Body] CreateReminderRequest request);

    [Put("/api/reminders/{id}")]
    Task<ReminderResponse> UpdateReminderAsync(string id, [Body] UpdateReminderRequest request);

    [Delete("/api/reminders/{id}")]
    Task DeleteReminderAsync(string id);

    // User Profile
    [Get("/api/user/profile")]
    Task<UserResponse> GetProfileAsync();

    [Put("/api/user/profile")]
    Task<UserResponse> UpdateProfileAsync([Body] UpdateProfileRequest request);
}
```

## Create Services/Api/RefitConfiguration.cs

```csharp
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
```

**STOP HERE - Confirm API interface and configuration are created before proceeding to Step 6**
