using QueueApp.Services.Api.Auth.Models;

namespace QueueApp.Services.Auth;

public interface IAuthService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetUserIdAsync();
    Task<AuthTokenResponse> SignInAsync(string email, string password);
    Task<AuthTokenResponse> SignUpAsync(string email, string password, string displayName, string phone);
    Task<bool> IsPhoneAvailableAsync(string phone);
    Task<bool> EnsureValidSessionAsync();
    Task SetSessionAsync(string accessToken, string? refreshToken, int expiresInSeconds);
    Task ClearSessionAsync();
    Task<bool> IsAuthenticatedAsync();
}
