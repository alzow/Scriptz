using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Models.Api.Responses;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;

namespace ScriptzApp.Services.Stubs;

/// <summary>
/// Stub auth service. Accepts any non-empty credentials and persists a fake
/// token to SecureStorage so IsAuthenticatedAsync() works correctly.
/// </summary>
public class StubAuthService : IAuthService
{
    private readonly ISecureStorageService _secureStorage;
    private const string TokenKey = "auth_token";

    public StubAuthService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        await Task.Delay(600); // simulate network latency

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var token = "stub-" + Guid.NewGuid();
        await _secureStorage.SetAsync(TokenKey, token);

        return new AuthResponse
        {
            Token        = token,
            RefreshToken = "stub-refresh-" + Guid.NewGuid(),
            User = new UserResponse
            {
                Id          = "user-1",
                Email       = request.Email,
                FirstName   = "Ahmed",
                LastName    = "Cassim",
                PhoneNumber = "072 000 0000",
                CreatedAt   = DateTime.UtcNow,
            },
        };
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        await Task.Delay(800); // simulate network latency

        var token = "stub-" + Guid.NewGuid();
        await _secureStorage.SetAsync(TokenKey, token);

        return new AuthResponse
        {
            Token        = token,
            RefreshToken = "stub-refresh-" + Guid.NewGuid(),
            User = new UserResponse
            {
                Id          = "user-new",
                Email       = request.Email,
                FirstName   = request.FirstName,
                LastName    = request.LastName,
                PhoneNumber = request.PhoneNumber,
                CreatedAt   = DateTime.UtcNow,
            },
        };
    }

    public async Task<bool> LogoutAsync()
    {
        await _secureStorage.RemoveAsync(TokenKey);
        return true;
    }

    public Task<string?> GetTokenAsync()
        => _secureStorage.GetAsync(TokenKey);

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}
