using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BusinessDetail.Flow;

public sealed record NextOpening(string Label, string TimeText);

// businesses has no opening-hours columns and there is no business_hours table, so trading hours are
// aggregated from operator_availability — the union across the business's active operators. A shop
// that has never filled in its hours has no data here at all, which callers have to handle rather
// than guess around: HasData is false and the mode line drops the hours half.
public sealed class BusinessHours
{
    // Display order runs Monday-first; day_of_week itself is Postgres's 0=Sunday.
    private static readonly int[] DisplayOrder = { 1, 2, 3, 4, 5, 6, 0 };
    private static readonly string[] DayAbbreviations = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

    private readonly Dictionary<int, (TimeSpan Start, TimeSpan End)> _byDay;

    private BusinessHours(Dictionary<int, (TimeSpan Start, TimeSpan End)> byDay)
    {
        _byDay = byDay;
        SummaryText = BuildSummary(byDay);
    }

    public static readonly BusinessHours Unknown = new(new Dictionary<int, (TimeSpan, TimeSpan)>());

    public bool HasData => _byDay.Count > 0;

    public string SummaryText { get; }

    public static BusinessHours FromAvailability(IEnumerable<OperatorAvailabilityResponse> windows)
    {
        var byDay = new Dictionary<int, (TimeSpan Start, TimeSpan End)>();

        foreach (var window in windows)
        {
            if (window.DayOfWeek is < 0 or > 6)
                continue;

            if (byDay.TryGetValue(window.DayOfWeek, out var existing))
            {
                byDay[window.DayOfWeek] = (
                    window.StartTime < existing.Start ? window.StartTime : existing.Start,
                    window.EndTime > existing.End ? window.EndTime : existing.End);
            }
            else
            {
                byDay[window.DayOfWeek] = (window.StartTime, window.EndTime);
            }
        }

        return byDay.Count == 0 ? Unknown : new BusinessHours(byDay);
    }

    public bool IsOpenAt(DateTime localNow)
    {
        if (!_byDay.TryGetValue((int)localNow.DayOfWeek, out var window))
            return false;

        var time = localNow.TimeOfDay;
        return time >= window.Start && time < window.End;
    }

    public TimeSpan? ClosingTimeOn(DateTime localDate) =>
        _byDay.TryGetValue((int)localDate.DayOfWeek, out var window) ? window.End : null;

    public NextOpening? FindNextOpening(DateTime localNow)
    {
        for (var offset = 0; offset <= 7; offset++)
        {
            var day = (int)localNow.AddDays(offset).DayOfWeek;
            if (!_byDay.TryGetValue(day, out var window))
                continue;

            if (offset == 0 && localNow.TimeOfDay >= window.Start)
                continue;

            var label = offset switch
            {
                0 => "Opens today",
                1 => "Opens tomorrow",
                _ => $"Opens {DayAbbreviations[day]}",
            };

            return new NextOpening(label, FormatClock(window.Start));
        }

        return null;
    }

    private static string BuildSummary(Dictionary<int, (TimeSpan Start, TimeSpan End)> byDay)
    {
        if (byDay.Count == 0)
            return string.Empty;

        var runs = new List<string>();
        var runStart = -1;
        var runEnd = -1;
        (TimeSpan Start, TimeSpan End) runHours = default;

        foreach (var day in DisplayOrder)
        {
            var hasDay = byDay.TryGetValue(day, out var hours);

            if (runStart >= 0 && (!hasDay || hours != runHours))
            {
                runs.Add(FormatRun(runStart, runEnd, runHours));
                runStart = -1;
            }

            if (!hasDay)
                continue;

            if (runStart < 0)
            {
                runStart = day;
                runHours = hours;
            }

            runEnd = day;
        }

        if (runStart >= 0)
            runs.Add(FormatRun(runStart, runEnd, runHours));

        return string.Join(" · ", runs);
    }

    private static string FormatRun(int firstDay, int lastDay, (TimeSpan Start, TimeSpan End) hours)
    {
        var days = firstDay == lastDay
            ? DayAbbreviations[firstDay]
            : $"{DayAbbreviations[firstDay]}–{DayAbbreviations[lastDay]}";

        return $"{days} {FormatRange(hours.Start)}–{FormatRange(hours.End)}";
    }

    private static string FormatRange(TimeSpan time) => $"{time.Hours}:{time.Minutes:00}";

    public static string FormatClock(TimeSpan time) => $"{time.Hours:00}:{time.Minutes:00}";
}
