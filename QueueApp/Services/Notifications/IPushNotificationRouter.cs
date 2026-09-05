namespace QueueApp.Services.Notifications;

public interface IPushNotificationRouter
{
    bool HasPendingTap { get; }

    void OnNotificationTapped(IDictionary<string, string>? data);

    void NotifyTabsReady();
}
