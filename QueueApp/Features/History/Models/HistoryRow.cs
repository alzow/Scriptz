using QueueApp.Features.CategoryPicker.Models;
using QueueApp.Framework.Theming;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Features.History.Models;

public enum HistoryRowKind
{
    Visit,
    Booking,
}

public sealed class HistoryRow
{
    public required HistoryRowKind Kind { get; init; }
    public required Guid BusinessId { get; init; }
    public required string BusinessName { get; init; }
    public required string CategoryIcon { get; init; }
    public required string MetaLine { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required string TimeText { get; init; }
    public required string StatusText { get; init; }
    public required Color StatusFill { get; init; }
    public required Color StatusInk { get; init; }
    public required Color StatusStroke { get; init; }
    public required double StatusStrokeThickness { get; init; }
    public required bool ShowWarningIcon { get; init; }

    private static string IconFor(string category) =>
        CategoryCatalog.All.FirstOrDefault(c => c.Key == category)?.IconSource ?? "ic_other";

    public static HistoryRow FromVisit(VisitResponse visit)
    {
        var meta = string.IsNullOrEmpty(visit.ServiceLabel)
            ? $"Queue · {visit.OperatorName}"
            : $"Queue · {visit.ServiceLabel} with {visit.OperatorName}";

        var occurredAt = new DateTimeOffset(DateTime.SpecifyKind(visit.VisitedAt, DateTimeKind.Utc));

        return new HistoryRow
        {
            Kind = HistoryRowKind.Visit,
            BusinessId = visit.BusinessId,
            BusinessName = visit.BusinessName,
            CategoryIcon = IconFor(visit.Category),
            MetaLine = meta,
            OccurredAt = occurredAt,
            TimeText = FormatTime(occurredAt),
            StatusText = "SERVED",
            StatusFill = HistoryStatusPalette.RaisedFill,
            StatusInk = HistoryStatusPalette.MutedInk,
            StatusStroke = Colors.Transparent,
            StatusStrokeThickness = 0,
            ShowWarningIcon = false,
        };
    }

    public static HistoryRow FromBooking(UpcomingBookingResponse booking)
    {
        var meta = string.IsNullOrEmpty(booking.ServiceName)
            ? $"Booking · {booking.OperatorName}"
            : $"Booking · {booking.ServiceName} with {booking.OperatorName}";

        if (booking.HasCancellationReason)
            meta = $"{meta} · {booking.CancellationReason}";

        var (statusText, fill, ink, outline, warn) = booking.EffectiveStatus switch
        {
            "confirmed" => ("CONFIRMED", HistoryStatusPalette.LiveFill, HistoryStatusPalette.LiveInk, false, false),
            "pending" => ("PENDING", HistoryStatusPalette.InfoFill, HistoryStatusPalette.InfoInk, false, false),
            "cancelled" => ("CANCELLED", Colors.Transparent, HistoryStatusPalette.DimInk, true, false),
            "completed" or "done" => ("COMPLETED", HistoryStatusPalette.RaisedFill, HistoryStatusPalette.MutedInk, false, false),
            "expired" => ("EXPIRED", Colors.Transparent, HistoryStatusPalette.DimInk, true, true),
            "no_show" => ("NO-SHOW", HistoryStatusPalette.BadFill, HistoryStatusPalette.BadInk, false, false),
            _ => (booking.Status.ToUpperInvariant(), HistoryStatusPalette.RaisedFill, HistoryStatusPalette.MutedInk, false, false),
        };

        return new HistoryRow
        {
            Kind = HistoryRowKind.Booking,
            BusinessId = booking.BusinessId,
            BusinessName = booking.BusinessName,
            CategoryIcon = IconFor(booking.Category),
            MetaLine = meta,
            OccurredAt = booking.StartsAt,
            TimeText = FormatTime(booking.StartsAt),
            StatusText = statusText,
            StatusFill = fill,
            StatusInk = ink,
            StatusStroke = outline ? HistoryStatusPalette.OutlineStroke : Colors.Transparent,
            StatusStrokeThickness = outline ? 1 : 0,
            ShowWarningIcon = warn,
        };
    }

    private static string FormatTime(DateTimeOffset value)
    {
        var local = value.ToOffset(TimeSpan.FromHours(2));
        var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).Date;

        if (local.Date == today)
            return local.ToString("h:mm tt");
        if (local.Date >= today.AddDays(-6) && local.Date < today)
            return local.ToString("ddd");
        return local.ToString("d MMM");
    }
}

internal static class HistoryStatusPalette
{
    public static Color RaisedFill => ThemePalette.Raised;
    public static Color MutedInk => ThemePalette.TextMuted;
    public static Color DimInk => ThemePalette.TextDim;
    public static Color OutlineStroke => ThemePalette.Border;

    // The pill fills are solid tint tokens now. They used to be the ink at 13-15% alpha, which
    // over a light surface is barely a colour at all, and composited differently on a card than
    // on the page.
    public static Color LiveInk => ThemePalette.AccentText;
    public static Color LiveFill => ThemePalette.AccentTint;

    public static Color InfoInk => ThemePalette.PurpleText;
    public static Color InfoFill => ThemePalette.PurpleTint;

    public static Color BadInk => ThemePalette.DangerText;
    public static Color BadFill => ThemePalette.DangerTint;
}
