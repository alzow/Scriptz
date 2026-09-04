using QueueApp.Features.History.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.History.Helpers;

public static class HistoryHelper
{
    public const string TodayBucket = "TODAY";
    public const string YesterdayBucket = "YESTERDAY";
    public const string ThisWeekBucket = "THIS WEEK";

    private const int WeekLengthInDays = 7;

    public static IEnumerable<HistoryGroup> BucketByDate(List<HistoryRow> rows)
    {
        var today = LocalTime.Now.Date;
        var weekStart = today.AddDays(-(WeekLengthInDays - 1));

        return rows
            .GroupBy(r => BucketKey(LocalTime.ToLocal(r.OccurredAt).Date, today, weekStart))
            .Select(g => new HistoryGroup(g.Key, g));
    }

    public static string BucketKey(DateTime date, DateTime today, DateTime weekStart)
    {
        if (date == today)
            return TodayBucket;

        if (date == today.AddDays(-1))
            return YesterdayBucket;

        return date >= weekStart
            ? ThisWeekBucket
            : date.ToString("MMMM").ToUpperInvariant();
    }
}
