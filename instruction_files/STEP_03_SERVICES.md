# STEP 3: Create Services Layer

This step creates all services (Storage, Popup, Auth, API).

## Create Directory Structure:

```bash
mkdir -p Services/Storage
mkdir -p Services/Popup
mkdir -p Services/Auth
mkdir -p Services/Api
```

## Create Services/Storage/ISecureStorageService.cs

```csharp
namespace ScriptzApp.Services.Storage;

public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<bool> RemoveAsync(string key);
    Task RemoveAllAsync();
}
```

## Create Services/Storage/SecureStorageService.cs

```csharp
namespace ScriptzApp.Services.Storage;

public class SecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage Get error: {ex.Message}");
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage Set error: {ex.Message}");
        }
    }

    public Task<bool> RemoveAsync(string key)
    {
        try
        {
            return Task.FromResult(SecureStorage.Default.Remove(key));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage Remove error: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task RemoveAllAsync()
    {
        try
        {
            SecureStorage.Default.RemoveAll();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage RemoveAll error: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
```

## Create Services/Popup/IScriptzPopupService.cs

```csharp
namespace ScriptzApp.Services.Popup;

public interface IScriptzPopupService
{
    Task ShowAlertAsync(string title, string message, string button = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task ShowLoadingAsync(string message = "Loading...");
    Task HideLoadingAsync();
}
```

## Create Services/Popup/ScriptzPopupService.cs

```csharp
using MPowerKit.Popups;

namespace ScriptzApp.Services.Popup;

public class ScriptzPopupService : IScriptzPopupService
{
    private readonly IPopupService _popupService;

    public ScriptzPopupService(IPopupService popupService)
    {
        _popupService = popupService;
    }

    public async Task ShowAlertAsync(string title, string message, string button = "OK")
    {
        await Application.Current!.MainPage!.DisplayAlert(title, message, button);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        return await Application.Current!.MainPage!.DisplayAlert(title, message, accept, cancel);
    }

    public Task ShowLoadingAsync(string message = "Loading...")
    {
        // Implement custom loading popup if needed
        return Task.CompletedTask;
    }

    public Task HideLoadingAsync()
    {
        // Implement hiding loading popup
        return Task.CompletedTask;
    }
}
```

## Create Services/Auth/IAuthService.cs

```csharp
using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Models.Api.Responses;

namespace ScriptzApp.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<bool> LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}
```

## Create Services/Auth/AuthService.cs

```csharp
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
```

## Create Services/Api/IApiService.cs

```csharp
namespace ScriptzApp.Services.Api;

public interface IApiService
{
    IScriptzApi Api { get; }
}
```

## Create Services/Api/ApiService.cs

```csharp
namespace ScriptzApp.Services.Api;

public class ApiService : IApiService
{
    public IScriptzApi Api { get; }

    public ApiService(IScriptzApi api)
    {
        Api = api;
    }
}
```

**STOP HERE - Confirm all service files are created before proceeding to Step 4**
