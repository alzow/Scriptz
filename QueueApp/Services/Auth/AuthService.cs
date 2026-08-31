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

    public AuthService(ISecureStorageService secureStorage, IAuthApi authApi)
    {
        _secureStorage = secureStorage;
        _authApi = authApi;
    }

    public Task<string?> GetAccessTokenAsync() => _secureStorage.GetAsync(SupabaseConfig.AccessTokenKey);

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

    public async Task<bool> EnsureValidSessionAsync()
    {
        var token = await _secureStorage.GetAsync(SupabaseConfig.AccessTokenKey);
        var refreshToken = await _secureStorage.GetAsync(SupabaseConfig.RefreshTokenKey);
        var expiryRaw = await _secureStorage.GetAsync(SupabaseConfig.TokenExpiryKey);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
            return false;

        var stillValid = DateTime.TryParse(
            expiryRaw,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var expiryUtc)
            && expiryUtc > DateTime.UtcNow.AddSeconds(60);

        if (stillValid)
            return true;

        try
        {
            var response = await _authApi.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });
            await SetSessionAsync(response.AccessToken, response.RefreshToken, response.ExpiresIn);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token refresh failed: {ex.Message}");
            await ClearSessionAsync();
            return false;
        }
    }

    public async Task SetSessionAsync(string accessToken, string? refreshToken, int expiresInSeconds)
    {
        await _secureStorage.SetAsync(SupabaseConfig.AccessTokenKey, accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
            await _secureStorage.SetAsync(SupabaseConfig.RefreshTokenKey, refreshToken);

        var expiryUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        await _secureStorage.SetAsync(SupabaseConfig.TokenExpiryKey, expiryUtc.ToString("O"));
    }

    public async Task ClearSessionAsync()
    {
        await _secureStorage.RemoveAsync(SupabaseConfig.AccessTokenKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.RefreshTokenKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.TokenExpiryKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.UserIdKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.UserEmailKey);
    }

    public Task<bool> IsAuthenticatedAsync() => EnsureValidSessionAsync();

    // TODO: no GoTrue /auth/v1/logout call yet, so the refresh token stays valid server-side after
    // this — device-local sign-out only, same gap IAuthApi.DeleteMyAccountAsync's TODO flags.
    public Task SignOutAsync() => ClearSessionAsync();

    public async Task DeleteAccountAsync()
    {
        await _authApi.DeleteMyAccountAsync();
        await ClearSessionAsync();
    }

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
