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
            await SetSessionAsync(response.AccessToken, response.RefreshToken);

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
                await SetSessionAsync(response.AccessToken, response.RefreshToken);

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

    public async Task SetSessionAsync(string accessToken, string? refreshToken)
    {
        await _secureStorage.SetAsync(SupabaseConfig.AccessTokenKey, accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
            await _secureStorage.SetAsync(SupabaseConfig.RefreshTokenKey, refreshToken);
    }

    public async Task ClearSessionAsync()
    {
        await _secureStorage.RemoveAsync(SupabaseConfig.AccessTokenKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.RefreshTokenKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.UserIdKey);
    }

    public async Task<bool> IsAuthenticatedAsync()
        => !string.IsNullOrEmpty(await GetAccessTokenAsync());
}
