namespace QueueApp.Services.Auth;

public interface IAuthService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetUserIdAsync();
    Task<bool> SignInAsync(string email, string password);
    Task<bool> SignUpAsync(string email, string password);
    Task<bool> EnsureValidSessionAsync();
    Task SetSessionAsync(string accessToken, string? refreshToken, int expiresInSeconds);
    Task ClearSessionAsync();
    Task<bool> IsAuthenticatedAsync();
}
