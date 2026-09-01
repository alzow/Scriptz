using System.Net;
using QueueApp.Constants;
using QueueApp.Services.Api.Auth;
using QueueApp.Services.Api.Auth.Models;
using QueueApp.Services.Storage;
using Refit;

namespace QueueApp.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ISecureStorageService _secureStorage;
    private readonly IAuthApi _authApi;
    private readonly ISessionRefreshService _sessionRefresh;

    public AuthService(
        ISecureStorageService secureStorage,
        IAuthApi authApi,
        ISessionRefreshService sessionRefresh)
    {
        _secureStorage = secureStorage;
        _authApi = authApi;
        _sessionRefresh = sessionRefresh;
    }

    public event EventHandler? SessionExpired
    {
        add => _sessionRefresh.SessionExpired += value;
        remove => _sessionRefresh.SessionExpired -= value;
    }

    public event EventHandler<string>? SessionRefreshed
    {
        add => _sessionRefresh.SessionRefreshed += value;
        remove => _sessionRefresh.SessionRefreshed -= value;
    }

    // Renews first if the token has expired or is about to, so a caller holding this token for a
    // long-lived connection (the realtime socket) isn't handed a dead one mid-session.
    public Task<string?> GetAccessTokenAsync() => _sessionRefresh.GetValidAccessTokenAsync();

    public Task<string?> GetUserIdAsync() => _secureStorage.GetAsync(SupabaseConfig.UserIdKey);

    // Cached at sign-in/sign-up; falls back to GoTrue for a session that predates that caching.
    public async Task<string?> GetUserEmailAsync()
    {
        var cached = await _secureStorage.GetAsync(SupabaseConfig.UserEmailKey);
        if (!string.IsNullOrWhiteSpace(cached))
            return cached;

        var user = await _authApi.GetUserAsync();
        if (string.IsNullOrWhiteSpace(user.Email))
            return null;

        await _secureStorage.SetAsync(SupabaseConfig.UserEmailKey, user.Email);
        return user.Email;
    }

    public async Task<AuthTokenResponse> SignInAsync(string email, string password)
    {
        var response = await _authApi.SignInAsync(new SignInRequest { Email = email, Password = password });
        await PersistSessionAsync(response);
        return response;
    }

    public async Task<AuthTokenResponse> SignUpAsync(string email, string password, string displayName, string phone)
    {
        var response = await _authApi.SignUpAsync(new SignUpRequest
        {
            Email = email,
            Password = password,
            Data = new SignUpMetadata
            {
                DisplayName = displayName,
                Phone = phone
            }
        });

        await PersistSessionAsync(response);
        return response;
    }

    public async Task<bool> IsPhoneAvailableAsync(string phone)
    {
        try
        {
            return await _authApi.IsPhoneAvailableAsync(new PhoneCheckRequest { Phone = phone });
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound
                                          or HttpStatusCode.Unauthorized
                                          or HttpStatusCode.Forbidden)
        {
            // TODO: drop this fallback once is_phone_available is deployed to every environment.
            System.Diagnostics.Debug.WriteLine($"Phone availability check unavailable: {ex.StatusCode}");
            return true;
        }
    }

    // Startup check. Expiry during the session is handled where the calls are made — see
    // SupabaseAuthHeaderHandler — so this no longer has to be the only place a token is renewed.
    public async Task<bool> EnsureValidSessionAsync()
    {
        var refreshToken = await _secureStorage.GetAsync(SupabaseConfig.RefreshTokenKey);
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        var token = await _sessionRefresh.GetValidAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public Task SetSessionAsync(string accessToken, string? refreshToken, int expiresInSeconds)
        => _sessionRefresh.StoreSessionAsync(accessToken, refreshToken, expiresInSeconds);

    public async Task ClearSessionAsync()
    {
        await _sessionRefresh.ClearSessionAsync();
        await _secureStorage.RemoveAsync(SupabaseConfig.UserIdKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.UserEmailKey);
    }

    public Task<bool> IsAuthenticatedAsync() => EnsureValidSessionAsync();

    // TODO: no GoTrue /auth/v1/logout call yet, so the refresh token stays valid server-side after
    // this — device-local sign-out only.
    public Task SignOutAsync() => ClearSessionAsync();

    private async Task PersistSessionAsync(AuthTokenResponse response)
    {
        if (string.IsNullOrEmpty(response.AccessToken))
            return;

        await SetSessionAsync(response.AccessToken, response.RefreshToken, response.ExpiresIn);

        if (response.User is not null)
        {
            await _secureStorage.SetAsync(SupabaseConfig.UserIdKey, response.User.Id.ToString());
            if (!string.IsNullOrWhiteSpace(response.User.Email))
                await _secureStorage.SetAsync(SupabaseConfig.UserEmailKey, response.User.Email);
        }
    }
}
