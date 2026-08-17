using ScriptzApp.Services.Storage;

namespace ScriptzApp.Services.Auth;

// Token store only, for now. Step 5 adds the actual Supabase OTP sign-in that calls SetSessionAsync.
public class AuthService : IAuthService
{
    private readonly ISecureStorageService _secureStorage;
    private const string TokenKey = "sb_access_token";
    private const string RefreshTokenKey = "sb_refresh_token";

    public AuthService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public Task<string?> GetAccessTokenAsync() => _secureStorage.GetAsync(TokenKey);

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
    }

    public async Task<bool> IsAuthenticatedAsync()
        => !string.IsNullOrEmpty(await GetAccessTokenAsync());
}
