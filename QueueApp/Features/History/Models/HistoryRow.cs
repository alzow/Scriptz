using QueueApp.Features.CategoryPicker.Models;
using QueueApp.Framework.Theming;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.History.Models;

public enum HistoryRowKind
{
    Visit,
    Booking,
}

public sealed class HistoryRow
{
    public required HistoryRowKind Kind { get; init; }
    public required Guid RecordId { get; init; }
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

    // The status text is the business's own vocabulary now, so "is this still to come?" cannot be
    // read back off it — the mapping that produced the text says so instead.
    public required bool IsUpcoming { get; init; }

    private static string IconFor(string category) =>
        CategoryCatalog.All.FirstOrDefault(c => c.Key == category)?.IconSource ?? "ic_other";

    public static HistoryRow FromEntry(MyQueueEntryResponse entry)
    {
        var meta = string.IsNullOrEmpty(entry.ServiceName)
            ? $"Queue · {entry.OperatorName}"
            : $"Queue · {entry.ServiceName} with {entry.OperatorName}";

        var occurredAt = entry.DoneAtUtc ?? entry.JoinedAtUtc;

        var (statusText, fill, ink, outline, warn, upcoming) = ResolveEntryStatus(entry, CategoryLabels.Resolve(entry.Category));

        return new HistoryRow
        {
            Kind = HistoryRowKind.Visit,
            RecordId = entry.Id,
            BusinessId = entry.BusinessId,
            BusinessName = entry.BusinessName,
            CategoryIcon = IconFor(entry.Category),
            MetaLine = meta,
            OccurredAt = occurredAt,
            TimeText = FormatTime(occurredAt),
            StatusText = statusText,
            StatusFill = fill,
            StatusInk = ink,
            StatusStroke = outline ? HistoryStatusPalette.OutlineStroke : Colors.Transparent,
            StatusStrokeThickness = outline ? 1 : 0,
            ShowWarningIcon = warn,
            IsUpcoming = upcoming,
        };
    }

    public static HistoryRow FromBooking(UpcomingBookingResponse booking)
    {
        var meta = string.IsNullOrEmpty(booking.ServiceName)
            ? $"Booking · {booking.OperatorName}"
            : $"Booking · {booking.ServiceName} with {booking.OperatorName}";

        if (booking.HasCancellationReason)
            meta = $"{meta} · {booking.CancellationReason}";

        var (statusText, fill, ink, outline, warn, upcoming) = booking.EffectiveStatus switch
        {
            "confirmed" => ("CONFIRMED", HistoryStatusPalette.LiveFill, HistoryStatusPalette.LiveInk, false, false, true),
            "pending" => ("PENDING", HistoryStatusPalette.InfoFill, HistoryStatusPalette.InfoInk, false, false, true),
            "cancelled" => (booking.WasCancelledByCustomer ? "YOU CANCELLED" : "CANCELLED",
                Colors.Transparent, HistoryStatusPalette.DimInk, true, false, false),
            "completed" or "done" => ("COMPLETED", HistoryStatusPalette.RaisedFill, HistoryStatusPalette.MutedInk, false, false, false),
            "expired" => ("EXPIRED", Colors.Transparent, HistoryStatusPalette.DimInk, true, true, false),
            "no_show" => ("NO-SHOW", HistoryStatusPalette.BadFill, HistoryStatusPalette.BadInk, false, false, false),
            "awaiting_collection" => ("READY FOR COLLECTION", HistoryStatusPalette.InfoFill, HistoryStatusPalette.InfoInk, false, false, true),
            _ => (booking.Status.ToUpperInvariant(), HistoryStatusPalette.RaisedFill, HistoryStatusPalette.MutedInk, false, false, false),
        };

        return new HistoryRow
        {
            Kind = HistoryRowKind.Booking,
            RecordId = booking.Id,
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
            IsUpcoming = upcoming,
        };
    }

    private static (string Text, Color Fill, Color Ink, bool Outline, bool Warn, bool Upcoming) ResolveEntryStatus(
        MyQueueEntryResponse entry,
        CategoryLabelSet labels)
    {
        if (entry.IsNoShow)
            return ("NO-SHOW", HistoryStatusPalette.BadFill, HistoryStatusPalette.BadInk, false, false, false);

        if (entry.IsCancelled)
            return (entry.Details?.CancelledBy == CancelledByValues.Customer ? "YOU LEFT" : "CANCELLED",
                Colors.Transparent, HistoryStatusPalette.DimInk, true, false, false);

        if (entry.IsAwaitingCollection)
            return ("READY FOR COLLECTION", HistoryStatusPalette.InfoFill, HistoryStatusPalette.InfoInk, false, false, true);

        if (entry.IsFinished)
            return ("SERVED", HistoryStatusPalette.RaisedFill, HistoryStatusPalette.MutedInk, false, false, false);

        return entry.IsBeingServed
            ? (labels.ServingStatus, HistoryStatusPalette.LiveFill, HistoryStatusPalette.LiveInk, false, false, true)
            : ("IN THE QUEUE", HistoryStatusPalette.LiveFill, HistoryStatusPalette.LiveInk, false, false, true);
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

    public static Color LiveInk => ThemePalette.AccentText;
    public static Color LiveFill => ThemePalette.AccentTint;

    public static Color InfoInk => ThemePalette.PurpleText;
    public static Color InfoFill => ThemePalette.PurpleTint;

    public static Color BadInk => ThemePalette.DangerText;
    public static Color BadFill => ThemePalette.DangerTint;
}
