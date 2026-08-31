namespace QueueApp.Services.Notifications;

public interface INotificationPermissionService
{
    Task<bool> IsAllowedAsync();

    // Prompts the OS permission dialog where one exists. Returns the resulting allowed state.
    Task<bool> RequestAsync();

    // The one recovery path once the OS dialog has already been dismissed once — Android and iOS
    // both refuse to show it again, so "allow" from there on only exists in Settings.
    void OpenAppSettings();
}
