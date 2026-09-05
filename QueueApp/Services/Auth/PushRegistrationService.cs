using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;
using QueueApp.Services.Api.Auth;
using QueueApp.Services.Api.Auth.Models;

namespace QueueApp.Services.Auth;

public class PushRegistrationService : IPushRegistrationService
{
    private readonly IDeviceTokenApi _api;
    private readonly IDeviceIdentityService _deviceIdentity;

    public PushRegistrationService(
        IDeviceTokenApi api,
        IDeviceIdentityService deviceIdentity)
    {
        _api = api;
        _deviceIdentity = deviceIdentity;
    }

    public async Task RegisterAsync()
    {
        // TEMP DIAGNOSTIC — remove once token registration is confirmed working
        Console.WriteLine("[Push] RegisterAsync entered");
        try
        {
            var permission = await Permissions.RequestAsync<Permissions.PostNotifications>();
            // TEMP DIAGNOSTIC — remove once token registration is confirmed working
            Console.WriteLine($"[Push] permission = {permission}");
            if (permission != PermissionStatus.Granted) return;

            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
            // TEMP DIAGNOSTIC — remove once token registration is confirmed working
            Console.WriteLine("[Push] CheckIfValid passed");

            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            // TEMP DIAGNOSTIC — remove once token registration is confirmed working
            Console.WriteLine($"[Push] token length = {token?.Length ?? 0}");
            if (string.IsNullOrWhiteSpace(token)) return;

            await SendAsync(token);
            // TEMP DIAGNOSTIC — remove once token registration is confirmed working
            Console.WriteLine("[Push] upsert sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Push] FAILED: {ex}");
        }
    }

    public async Task UnregisterAsync()
    {
        try
        {
            var deviceId = await _deviceIdentity.GetDeviceIdAsync();
            await _api.RemoveAsync(new RemoveDeviceTokenRequest(deviceId));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Push unregister failed: {ex}");
        }
    }

    public async void OnTokenRefreshed(object? sender, FCMTokenChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Token))
            return;

        try
        {
            await SendAsync(e.Token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token refresh save failed: {ex}");
        }
    }

    private async Task SendAsync(string token)
    {
        var deviceId = await _deviceIdentity.GetDeviceIdAsync();
        var platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";

        await _api.UpsertAsync(
            new UpsertDeviceTokenRequest(deviceId, token, platform));
    }
}