namespace QueueApp.Constants;

// The `action` value a notification row carries, sent through to the device in the FCM message's
// data payload. It is what the tap handler routes on, so a value added here must match the string
// the database trigger writes exactly.
public static class PushNotificationActions
{
    public const string OperatorQueue    = "operator_queue";
    public const string OperatorBookings = "operator_bookings";
    public const string QueueStatus      = "queue_status";
    public const string BookingDetail    = "booking_detail";
}
