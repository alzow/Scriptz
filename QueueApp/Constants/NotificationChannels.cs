namespace QueueApp.Constants;

// Must match the channel_id the send-side FCM payload uses (Step 24 Edge Function), or a push
// falls through to the SDK's own fcm_fallback_notification_channel — silent settings, generic name.
public static class NotificationChannels
{
    public const string QueueUpdatesId = "queue_updates";
    public const string QueueUpdatesName = "Queue updates";
    public const string QueueUpdatesDescription = "Your turn, booking changes, and collection alerts.";
}
