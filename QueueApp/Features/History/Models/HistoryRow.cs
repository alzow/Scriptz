using QueueApp.Features.CategoryPicker.Models;
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
    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];

    public static Color RaisedFill => Resource("SurfaceRaised");
    public static Color MutedInk => Resource("TextMuted");
    public static Color DimInk => Resource("TextFaint");
    public static Color OutlineStroke => Resource("Line");

    public static Color LiveInk => Resource("Green");
    public static Color LiveFill => LiveInk.WithAlpha(0.13f);

    public static Color InfoInk => Resource("Purple");
    public static Color InfoFill => InfoInk.WithAlpha(0.15f);

    public static Color BadInk => Resource("Danger");
    public static Color BadFill => BadInk.WithAlpha(0.13f);
}
