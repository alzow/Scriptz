namespace QueueApp.Services.Notifications;

// Device-local until Profile §14 decides whether these need to follow the customer across
// devices (a profiles column) rather than living in Preferences.
public class NotificationPreferences
{
    public bool TimeToLeave { get; set; } = true;
    public bool YoureNext { get; set; } = true;
    public bool QueueChanged { get; set; } = true;
    public bool BookingConfirmed { get; set; } = true;
    public bool BookingReminders { get; set; } = true;

    // TODO: server-side trigger pending Documentation/awaiting-collection-backend-requirements.md.
    public bool AwaitingCollectionReady { get; set; } = true;

    // The default nudge for a business the customer hasn't set a per-shop travel time for yet.
    public int LeaveAtMinutes { get; set; } = 10;

    public int OnCount =>
        (TimeToLeave ? 1 : 0) + (YoureNext ? 1 : 0) + (QueueChanged ? 1 : 0) +
        (BookingConfirmed ? 1 : 0) + (BookingReminders ? 1 : 0) + (AwaitingCollectionReady ? 1 : 0);

    public const int TotalCount = 6;
}
