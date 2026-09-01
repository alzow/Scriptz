using QueueApp.Services.Api.Auth.Models;

namespace QueueApp.Services.Auth;

public interface IAuthService
{
    // The session could not be renewed and has been cleared — the user has to sign in again.
    event EventHandler? SessionExpired;

    // The access token was renewed mid-session; carries the new one for anything holding a
    // connection that was authorised with the old token.
    event EventHandler<string>? SessionRefreshed;

    // Renews the token first if it has expired or is about to.
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetUserIdAsync();
    Task<string?> GetUserEmailAsync();
    Task<AuthTokenResponse> SignInAsync(string email, string password);
    Task<AuthTokenResponse> SignUpAsync(string email, string password, string displayName, string phone);
    Task<bool> IsPhoneAvailableAsync(string phone);
    Task<bool> EnsureValidSessionAsync();
    Task SetSessionAsync(string accessToken, string? refreshToken, int expiresInSeconds);
    Task ClearSessionAsync();
    Task<bool> IsAuthenticatedAsync();

    // Signs out of this device only — a GoTrue-side revoke is left for later, see AuthService.
    Task SignOutAsync();
}
