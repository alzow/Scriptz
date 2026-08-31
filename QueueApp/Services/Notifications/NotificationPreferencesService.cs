namespace QueueApp.Services.Notifications;

// Same Preferences.Default shape as ThemeService: the OS switch being off must never wipe what the
// customer already told the app they want, so reads and writes stay independent of permission state.
public class NotificationPreferencesService : INotificationPreferencesService
{
    private const string TimeToLeaveKey = "notif_time_to_leave";
    private const string YoureNextKey = "notif_youre_next";
    private const string QueueChangedKey = "notif_queue_changed";
    private const string BookingConfirmedKey = "notif_booking_confirmed";
    private const string BookingRemindersKey = "notif_booking_reminders";
    private const string LeaveAtMinutesKey = "notif_leave_at_minutes";

    public NotificationPreferences Get()
    {
        var defaults = new NotificationPreferences();
        try
        {
            return new NotificationPreferences
            {
                TimeToLeave = Preferences.Default.Get(TimeToLeaveKey, defaults.TimeToLeave),
                YoureNext = Preferences.Default.Get(YoureNextKey, defaults.YoureNext),
                QueueChanged = Preferences.Default.Get(QueueChangedKey, defaults.QueueChanged),
                BookingConfirmed = Preferences.Default.Get(BookingConfirmedKey, defaults.BookingConfirmed),
                BookingReminders = Preferences.Default.Get(BookingRemindersKey, defaults.BookingReminders),
                LeaveAtMinutes = Preferences.Default.Get(LeaveAtMinutesKey, defaults.LeaveAtMinutes),
            };
        }
        catch (Exception)
        {
            return defaults;
        }
    }

    public void Save(NotificationPreferences preferences)
    {
        try
        {
            Preferences.Default.Set(TimeToLeaveKey, preferences.TimeToLeave);
            Preferences.Default.Set(YoureNextKey, preferences.YoureNext);
            Preferences.Default.Set(QueueChangedKey, preferences.QueueChanged);
            Preferences.Default.Set(BookingConfirmedKey, preferences.BookingConfirmed);
            Preferences.Default.Set(BookingRemindersKey, preferences.BookingReminders);
            Preferences.Default.Set(LeaveAtMinutesKey, preferences.LeaveAtMinutes);
        }
        catch (Exception)
        {
        }
    }
}
