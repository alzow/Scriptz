namespace QueueApp.Services.Notifications;

// Wraps the OS notification switch the same way LocationService wraps location: check first,
// request only if it hasn't been decided, and never throw out to a screen that wants a bool.
//
// The check deliberately asks "will a message actually reach this customer", not "was the runtime
// permission granted" — on Android those differ the moment someone turns Queue off in Settings, and
// a Profile tab that says "You'll be told when to leave" while the phone drops everything is the
// exact failure this screen exists to prevent.
public class NotificationPermissionService : INotificationPermissionService
{
    public async Task<bool> IsAllowedAsync()
    {
        try
        {
#if ANDROID
            return await Task.FromResult(
                AndroidX.Core.App.NotificationManagerCompat.From(Platform.AppContext).AreNotificationsEnabled());
#elif IOS || MACCATALYST
            var settings = await UserNotifications.UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
            return settings.AuthorizationStatus is UserNotifications.UNAuthorizationStatus.Authorized
                or UserNotifications.UNAuthorizationStatus.Provisional
                or UserNotifications.UNAuthorizationStatus.Ephemeral;
#else
            return await Task.FromResult(true);
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Notifications] permission check failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RequestAsync()
    {
        try
        {
#if ANDROID
            // No-op below API 33, where there is no runtime permission to ask for — the Settings
            // switch AreNotificationsEnabled() reads is the only thing that matters there.
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                await Permissions.RequestAsync<Permissions.PostNotifications>();

            return await IsAllowedAsync();
#elif IOS || MACCATALYST
            var (granted, _) = await UserNotifications.UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UserNotifications.UNAuthorizationOptions.Alert |
                UserNotifications.UNAuthorizationOptions.Sound |
                UserNotifications.UNAuthorizationOptions.Badge);

            return granted;
#else
            return await Task.FromResult(true);
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Notifications] permission request failed: {ex.Message}");
            return false;
        }
    }

    public void OpenAppSettings() => AppInfo.Current.ShowSettingsUI();
}
