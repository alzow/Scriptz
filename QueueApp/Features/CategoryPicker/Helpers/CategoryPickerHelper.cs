using QueueApp.Features.CategoryPicker.Models;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.CategoryPicker.Helpers;

public static class CategoryPickerHelper
{
    public const int FrequentBusinessLimit = 3;

    public const string UpdatedJustNow = "Updated just now";

    public static string FormatUpdatedAgo(DateTimeOffset resolvedAt)
    {
        var elapsed = DateTimeOffset.UtcNow - resolvedAt;

        if (elapsed < TimeSpan.FromMinutes(1))
            return UpdatedJustNow;

        if (elapsed < TimeSpan.FromHours(1))
            return $"Updated {TextFormat.Plural((int)elapsed.TotalMinutes, "minute")} ago";

        return elapsed < TimeSpan.FromDays(1)
            ? $"Updated {TextFormat.Plural((int)elapsed.TotalHours, "hour")} ago"
            : $"Updated {TextFormat.Plural((int)elapsed.TotalDays, "day")} ago";
    }

    // Finished entries only: somewhere the customer joined and then left is not somewhere they go
    // often, and done_at is the one stamp that says a visit actually happened.
    //
    // Scoped to the picked category the same way the businesses list is, so the two sections agree
    // on what the customer is looking for: with Barbers picked, a barber they go to every month is
    // relevant and the dentist they saw once is not. The filter is applied here rather than by
    // re-reading the visits, since the entries already carry their business's category.
    public static IEnumerable<FrequentBusinessItem> BuildFrequentBusinesses(
        List<MyQueueEntryResponse> visits,
        string? categoryKey = null) =>
        visits
            .Where(v => v.BusinessId != Guid.Empty && v.DoneAt is not null)
            .Where(v => string.IsNullOrEmpty(categoryKey)
                || string.Equals(v.Category, categoryKey, StringComparison.OrdinalIgnoreCase))
            .GroupBy(v => v.BusinessId)
            .Select(g =>
            {
                var latest = g.OrderByDescending(v => v.DoneAt).First();
                return new FrequentBusinessItem
                {
                    BusinessId = g.Key,
                    BusinessName = latest.BusinessName,
                    VisitCount = g.Count(),
                    LastVisitedAt = latest.DoneAt!.Value,
                    LastOperatorName = latest.OperatorName,
                    LastServiceLabel = latest.ServiceName,
                };
            })
            .OrderByDescending(f => f.VisitCount)
            .ThenByDescending(f => f.LastVisitedAt)
            .Take(FrequentBusinessLimit);
}
