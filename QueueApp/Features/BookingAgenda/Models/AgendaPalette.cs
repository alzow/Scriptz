using QueueApp.Framework.Theming;

namespace QueueApp.Features.BookingAgenda.Models;

/// <summary>
/// Agenda row colours, resolved per access so a theme switch reaches rows built after it.
/// The tints are solid tokens now rather than alpha over the row: an alpha tint composites to a
/// different colour depending on whether it lands on the page or on a card, and at 5-16% over a
/// light surface it disappears entirely.
/// </summary>
public static class AgendaPalette
{
    public static Color Surface => ThemePalette.Surface;
    public static Color SurfaceRaised => ThemePalette.Raised;
    public static Color Line => ThemePalette.Border;

    // The status bar down the left of a row is a fill, so it keeps the vivid brand colour.
    public static Color Green => ThemePalette.Accent;
    public static Color GreenBorder => ThemePalette.AccentBorder;
    public static Color GreenTint => ThemePalette.AccentTint;

    public static Color Purple => ThemePalette.Purple;
    public static Color PurpleBorder => ThemePalette.PurpleBorder;
    public static Color PurpleTint => ThemePalette.PurpleTint;

    public static Color Ink => ThemePalette.TextInk;
    public static Color Muted => ThemePalette.TextMuted;
    public static Color Dim => ThemePalette.TextDim;
    public static Color OnGreen => ThemePalette.TextOnAccent;

    public static Brush PurpleStroke => ThemePalette.Brush("PurpleText");
    public static Brush PurpleDimStroke => ThemePalette.Brush("PurpleBorder");
}
