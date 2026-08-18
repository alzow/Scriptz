using System.Diagnostics;
using QueueApp.Constants;
using QueueApp.Services.Api.Auth;
using QueueApp.Services.Api.Auth.Models;
using QueueApp.Services.Storage;

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

    public async Task<bool> SignInAsync(string email, string password)
    {
        try
        {
            var response = await _authApi.SignInAsync(new SignInRequest { Email = email, Password = password });
            await SetSessionAsync(response.AccessToken, response.RefreshToken, response.ExpiresIn);

            if (response.User is not null)
                await _secureStorage.SetAsync(SupabaseConfig.UserIdKey, response.User.Id.ToString());

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SignIn error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SignUpAsync(string email, string password)
    {
        try
        {
            var response = await _authApi.SignUpAsync(new SignUpRequest { Email = email, Password = password });

            if (!string.IsNullOrEmpty(response.AccessToken))
            {
                await SetSessionAsync(response.AccessToken, response.RefreshToken, response.ExpiresIn);

                if (response.User is not null)
                    await _secureStorage.SetAsync(SupabaseConfig.UserIdKey, response.User.Id.ToString());
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SignUp error: {ex.Message}");
            return false;
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
            Debug.WriteLine("Access token expired, attempting to refresh...");
            var response = await _authApi.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });
            await SetSessionAsync(response.AccessToken, response.RefreshToken, response.ExpiresIn);
            Debug.WriteLine("Refreshed Token successfully.");
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
    }

    public Task<bool> IsAuthenticatedAsync()
        => EnsureValidSessionAsync();
}
