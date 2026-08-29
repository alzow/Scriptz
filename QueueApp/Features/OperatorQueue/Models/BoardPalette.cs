using QueueApp.Framework.Theming;

namespace QueueApp.Features.OperatorQueue.Models;

/// <summary>
/// Board colours for the parts of the operator screen that are painted from C#. Properties, not
/// fields: a field would pin whichever theme was live when the class was first touched.
/// </summary>
public static class BoardPalette
{
    public static Color Ink => ThemePalette.TextInk;
    public static Color Muted => ThemePalette.TextMuted;

    // Purple as a label reads through PurpleText; as a fill it stays the vivid brand colour.
    public static Color Purple => ThemePalette.PurpleText;
    public static Color PurpleDim => ThemePalette.PurpleBorder;

    public static Brush PurpleStroke => ThemePalette.Brush("PurpleText");
    public static Brush PurpleDimStroke => ThemePalette.Brush("PurpleBorder");
    public static Brush GreenStroke => ThemePalette.Brush("AccentText");
    public static Brush LineStroke => ThemePalette.Brush("Border");
}
