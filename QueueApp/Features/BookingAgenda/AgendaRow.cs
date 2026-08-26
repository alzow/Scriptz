using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Features.BookingAgenda;

public enum AgendaRowKind
{
    Booking,
    Gap,
    Blocked,
}

// One item type for all three shapes the agenda mixes, discriminated by Kind — not three templates
// behind a DataTemplateSelector, which would split the CollectionView's recycling pool three ways
// on a list that already scrolls (spec §11).
//
// Everything a template binds to is worked out once here, at map time: no converters run per cell.
public sealed class AgendaRow : ObservableObject
{
    public required AgendaRowKind Kind { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }

    // The row this came from, when it came from one. Null for gaps and blocked ranges.
    public AgendaBookingResponse? Booking { get; init; }

    public bool IsBooking => Kind == AgendaRowKind.Booking;
    public bool IsGap => Kind == AgendaRowKind.Gap;
    public bool IsBlocked => Kind == AgendaRowKind.Blocked;

    public string TimeText { get; init; } = "";
    public string DurationText { get; init; } = "";
    public bool ShowDuration => DurationText.Length > 0;

    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";

    public string BayText { get; init; } = "";
    public bool ShowBay => BayText.Length > 0;

    public string TagText { get; init; } = "";
    public bool ShowTag => TagText.Length > 0;
    public Color TagTextColor { get; init; } = AgendaPalette.Ink;
    public Color TagBackgroundColor { get; init; } = AgendaPalette.Transparent;

    public Color BarColor { get; init; } = AgendaPalette.Line;
    public Color RowBackgroundColor { get; init; } = AgendaPalette.Surface;
    public Color RowStrokeColor { get; init; } = AgendaPalette.Line;
    public Color TitleColor { get; init; } = AgendaPalette.Ink;
    public double RowOpacity { get; init; } = 1;

    // Dashed reads as "not yet real" at a glance, which is exactly a pending booking's status —
    // and an empty stretch's, which nobody has bought yet. Null means a solid stroke.
    public DoubleCollection? RowStrokeDash { get; init; }

    public bool ShowFill => Kind == AgendaRowKind.Gap;

    // Tapping does something on a booking (the actions sheet) and on a gap (fill it); a blocked
    // range is managed from the block sheet that made it, so it swallows nothing and offers nothing.
    public bool IsNotTappable => Kind == AgendaRowKind.Blocked;

    // The now rule is part of a row rather than a fourth item type: it belongs to whichever row it
    // currently sits above, so the page timer moves it by flipping two booleans instead of
    // rebuilding the collection every minute.
    public bool ShowNowLineAbove { get; set; }
    public bool ShowNowLineBelow { get; set; }
    public string NowText { get; set; } = "";
}
