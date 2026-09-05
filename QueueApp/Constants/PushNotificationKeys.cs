namespace QueueApp.Constants;

public static class PushNotificationKeys
{
    // Top level of the FCM data payload.
    public const string Action       = "action";
    public const string ActionParams = "action_params";

    // Inside action_params, which arrives as a JSON string rather than an object: FCM data values
    // are always strings.
    public const string EntryId      = "entry_id";
    public const string BookingId    = "booking_id";
}
