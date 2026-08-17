namespace ScriptzApp.Services.Auth;

// Until Step 5 wires Supabase phone-OTP auth, this just holds/returns a token.
// GetAccessTokenAsync returns null when signed out -> the header handler falls back to the anon key.
public interface IAuthService
{
    Task<string?> GetAccessTokenAsync();
    Task SetSessionAsync(string accessToken, string? refreshToken);
    Task ClearSessionAsync();
    Task<bool> IsAuthenticatedAsync();
}
