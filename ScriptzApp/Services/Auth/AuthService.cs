using ScriptzApp.Services.Api.Auth;
using ScriptzApp.Services.Api.Auth.Models;
using ScriptzApp.Services.Storage;

namespace ScriptzApp.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ISecureStorageService _secureStorage;
    private readonly IAuthApi _authApi;
    private const string TokenKey = "sb_access_token";
    private const string RefreshTokenKey = "sb_refresh_token";
    private const string UserIdKey = "sb_user_id";

    public AuthService(ISecureStorageService secureStorage, IAuthApi authApi)
    {
        _secureStorage = secureStorage;
        _authApi = authApi;
    }

    public Task<string?> GetAccessTokenAsync() => _secureStorage.GetAsync(TokenKey);

    public Task<string?> GetUserIdAsync() => _secureStorage.GetAsync(UserIdKey);

    public async Task<bool> SignInAsync(string email, string password)
    {
        try
        {
            var response = await _authApi.SignInAsync(new SignInRequest { Email = email, Password = password });
            await SetSessionAsync(response.AccessToken, response.RefreshToken);

            if (response.User is not null)
                await _secureStorage.SetAsync(UserIdKey, response.User.Id.ToString());

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
                    await _secureStorage.SetAsync(UserIdKey, response.User.Id.ToString());
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
        await _secureStorage.SetAsync(TokenKey, accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
            await _secureStorage.SetAsync(RefreshTokenKey, refreshToken);
    }

    public async Task ClearSessionAsync()
    {
        await _secureStorage.RemoveAsync(TokenKey);
        await _secureStorage.RemoveAsync(RefreshTokenKey);
        await _secureStorage.RemoveAsync(UserIdKey);
    }

    public async Task<bool> IsAuthenticatedAsync()
        => !string.IsNullOrEmpty(await GetAccessTokenAsync());
}
