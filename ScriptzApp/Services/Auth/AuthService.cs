using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Models.Api.Responses;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Api;

namespace ScriptzApp.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IScriptzApi _api;
    private readonly ISecureStorageService _secureStorage;
    private const string TokenKey = "auth_token";
    private const string RefreshTokenKey = "refresh_token";

    public AuthService(IScriptzApi api, ISecureStorageService secureStorage)
    {
        _api = api;
        _secureStorage = secureStorage;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _api.LoginAsync(request);

            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                await _secureStorage.SetAsync(TokenKey, response.Token);

                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _secureStorage.SetAsync(RefreshTokenKey, response.RefreshToken);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _api.RegisterAsync(request);

            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                await _secureStorage.SetAsync(TokenKey, response.Token);

                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _secureStorage.SetAsync(RefreshTokenKey, response.RefreshToken);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Register error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> LogoutAsync()
    {
        await _secureStorage.RemoveAsync(TokenKey);
        await _secureStorage.RemoveAsync(RefreshTokenKey);
        return true;
    }

    public Task<string?> GetTokenAsync()
    {
        return _secureStorage.GetAsync(TokenKey);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}
