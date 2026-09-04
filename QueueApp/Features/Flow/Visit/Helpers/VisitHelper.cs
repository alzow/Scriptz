using QueueApp.Features.Flow.Visit.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.Flow.Visit.Helpers;

public static class VisitHelper
{
    public const string NotRecorded = "Not recorded";
    public const string NotYet = "Not yet";
    public const string NoTime = "--:--";
    public const string UnderAMinute = "under a minute";

    private const int DefaultCalendarMinutes = 30;

    // A queue entry only lacks an operator when the shop had nobody on shift to assign, and a
    // booking only until the shop picks who is taking it. Neither is a person, so neither gets
    // phrased as one.
    public static string WithWhom(VisitRecord record) => record.HasOperator
        ? $"with {record.OperatorName}"
        : record.IsQueue ? "with whoever's free first" : "with whoever's free at that time";

    public static string BuildShareText(VisitRecord record)
    {
        if (record.IsQueue)
        {
            return record.Position > 0
                ? $"I'm {TextFormat.Ordinal(record.Position)} in the queue at {record.BusinessName}."
                : $"I'm in the queue at {record.BusinessName}.";
        }

        return record.SlotStart is { } slot
            ? $"I'm booked at {record.BusinessName} on {LocalTime.Day(slot)} at {LocalTime.Time(slot)}."
            : $"I'm booked at {record.BusinessName}.";
    }

    public static string BuildCalendarEntry(VisitRecord record, string? address)
    {
        var start = record.SlotStart?.UtcDateTime ?? DateTime.UtcNow;
        var end = record.SlotEnd?.UtcDateTime ?? start.AddMinutes(DefaultCalendarMinutes);

        return string.Join("\r\n",
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Queue//EN",
            "BEGIN:VEVENT",
            $"UID:{record.Id}",
            $"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}",
            $"DTSTART:{start:yyyyMMdd'T'HHmmss'Z'}",
            $"DTEND:{end:yyyyMMdd'T'HHmmss'Z'}",
            $"SUMMARY:{record.ServiceName} at {record.BusinessName}",
            $"LOCATION:{address ?? record.BusinessName}",
            "END:VEVENT",
            "END:VCALENDAR");
    }

    public static VisitTimelineStep Step(DateTimeOffset? at, string text, VisitStepState state) => new()
    {
        MomentText = FormatMoment(at),
        Text = text,
        State = state,
    };

    public static VisitTimelineStep Pending(string text) => new()
    {
        MomentText = NotYet,
        Text = text,
        State = VisitStepState.Pending,
    };

    public static string FormatMoment(DateTimeOffset? instant) =>
        instant is { } value ? LocalTime.Moment(value) : NotRecorded;

    public static string FormatTime(DateTimeOffset? instant) =>
        instant is { } value ? LocalTime.Time(value) : NoTime;

    public static string FormatSlot(VisitRecord record) => record.SlotStart is { } start
        ? LocalTime.Range(start, record.SlotEnd)
        : string.Empty;

    public static string DescribeSpan(TimeSpan span)
    {
        var minutes = (int)Math.Round(span.TotalMinutes);

        if (minutes < 1)
            return UnderAMinute;

        if (minutes < 60)
            return $"{minutes} min";

        var hours = minutes / 60;
        var rest = minutes % 60;
        return rest == 0 ? $"{hours} hr" : $"{hours} hr {rest} min";
    }
}
