namespace QueueApp.Services.Notifications;

public interface INotificationPreferencesService
{
    NotificationPreferences Get();
    void Save(NotificationPreferences preferences);
}
