namespace QueueApp.Features.BusinessDetail.Flow;

// Same fixed +2 SAST conversion the booking models already use — SA has no DST, and the device
// clock can't be trusted to be on local time.
public static class LocalTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(2);

    public static DateTime Now => DateTimeOffset.UtcNow.ToOffset(Offset).DateTime;

    public static DateTimeOffset ToLocal(DateTimeOffset instant) => instant.ToOffset(Offset);
}
