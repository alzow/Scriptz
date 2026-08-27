using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Features.BookingAgenda.Models;

public enum AgendaRowKind
{
    Booking,
    Gap,
    Blocked,
}

public sealed class AgendaRow : ObservableObject
{
    public required AgendaRowKind Kind { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }

    public AgendaBookingResponse? Booking { get; init; }

    public bool IsBooking => Kind == AgendaRowKind.Booking;
    public bool IsGap => Kind == AgendaRowKind.Gap;
    public bool IsBlocked => Kind == AgendaRowKind.Blocked;
    public bool IsNotTappable => Kind == AgendaRowKind.Blocked;

    public string TimeText { get; init; } = string.Empty;
    public string DurationText { get; init; } = string.Empty;
    public bool ShowDuration => DurationText.Length > 0;

    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;

    public string BayText { get; init; } = string.Empty;
    public bool ShowBay => BayText.Length > 0;

    public string TagText { get; init; } = string.Empty;
    public bool ShowTag => TagText.Length > 0;
    public Color TagTextColor { get; init; } = AgendaPalette.Ink;
    public Color TagBackgroundColor { get; init; } = Colors.Transparent;

    public Color BarColor { get; init; } = AgendaPalette.Line;
    public Color RowBackgroundColor { get; init; } = AgendaPalette.Surface;
    public Color RowStrokeColor { get; init; } = AgendaPalette.Line;
    public Color TitleColor { get; init; } = AgendaPalette.Ink;
    public double RowOpacity { get; init; } = 1;

    public DoubleCollection? RowStrokeDash { get; init; }

    public bool ShowFill => Kind == AgendaRowKind.Gap;

    public bool ShowNowLineAbove { get; set; }
    public bool ShowNowLineBelow { get; set; }
    public string NowText { get; set; } = string.Empty;
}
