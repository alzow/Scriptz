namespace ScriptzApp.Services.Auth;

public interface IAuthService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetUserIdAsync();
    Task<bool> SignInAsync(string email, string password);
    Task<bool> SignUpAsync(string email, string password);
    Task SetSessionAsync(string accessToken, string? refreshToken);
    Task ClearSessionAsync();
    Task<bool> IsAuthenticatedAsync();
}
