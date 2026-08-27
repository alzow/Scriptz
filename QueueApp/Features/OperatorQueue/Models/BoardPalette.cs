namespace QueueApp.Features.OperatorQueue.Models;

public static class BoardPalette
{
    public static readonly Color Ink = Color.FromArgb("#F2F4F7");
    public static readonly Color Muted = Color.FromArgb("#8A8F98");
    public static readonly Color Purple = Color.FromArgb("#A45EFF");
    public static readonly Color PurpleDim = Color.FromArgb("#4A3670");

    public static readonly Brush PurpleStroke = new SolidColorBrush(Purple);
    public static readonly Brush PurpleDimStroke = new SolidColorBrush(PurpleDim);
    public static readonly Brush GreenStroke = new SolidColorBrush(Color.FromArgb("#39FF7A"));
    public static readonly Brush LineStroke = new SolidColorBrush(Color.FromArgb("#343D4D"));
}
