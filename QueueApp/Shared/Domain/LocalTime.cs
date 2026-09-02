using System.Globalization;

namespace QueueApp.Shared.Domain;

// Same fixed +2 SAST conversion the booking models already use — SA has no DST, and the device
// clock can't be trusted to be on local time.
public static class LocalTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(2);

    public static DateTime Now => DateTimeOffset.UtcNow.ToOffset(Offset).DateTime;

    public static DateTimeOffset ToLocal(DateTimeOffset instant) => instant.ToOffset(Offset);

    // Every face of a timestamp is formatted invariantly and against the fixed offset. The device's
    // culture would otherwise pick the day and month names, the digits and even the time separator,
    // so the same visit read "20:34" on one phone and "٨:٣٤ م" on the next.
    public static string Time(DateTimeOffset instant) =>
        ToLocal(instant).ToString("HH:mm", CultureInfo.InvariantCulture);

    // "Today" and "Yesterday" where they help, a dated day where they don't, and the year too once
    // the visit is old enough for it to matter.
    public static string Day(DateTimeOffset instant)
    {
        var day = ToLocal(instant).Date;
        var today = Now.Date;

        if (day == today)
            return "Today";

        if (day == today.AddDays(-1))
            return "Yesterday";

        if (day == today.AddDays(1))
            return "Tomorrow";

        return day.Year == today.Year
            ? day.ToString("ddd d MMM", CultureInfo.InvariantCulture)
            : day.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture);
    }

    // "Today · 20:34". The day always leads, because a bare time in a list of them is only readable
    // when every row happens to fall on the same date — and a visit's rarely do.
    public static string Moment(DateTimeOffset instant) => $"{Day(instant)} · {Time(instant)}";

    // "Sun 6 Sep · 16:00–17:00", or the open-ended half of it when there is no end.
    public static string Range(DateTimeOffset start, DateTimeOffset? end) =>
        end is { } finish
            ? $"{Day(start)} · {Time(start)}–{Time(finish)}"
            : Moment(start);
}
