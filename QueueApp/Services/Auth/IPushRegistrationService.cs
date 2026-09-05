using Plugin.Firebase.CloudMessaging.EventArgs;

namespace QueueApp.Services.Auth;

public interface IPushRegistrationService
{
    Task RegisterAsync();
    Task UnregisterAsync();
    void OnTokenRefreshed(object? sender, FCMTokenChangedEventArgs e);
}