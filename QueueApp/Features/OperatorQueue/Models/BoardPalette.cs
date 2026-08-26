namespace QueueApp.Features.OperatorQueue.Models;

// The handful of colours the board sets from code rather than XAML, so a value bound inside an
// item template is a plain property read rather than a converter running on every recycle.
// These mirror Resources/Styles/Colors.xaml — change them together.
public static class BoardPalette
{
    public static readonly Color Ink = Color.FromArgb("#F2F4F7");
    public static readonly Color Muted = Color.FromArgb("#8A8F98");
    public static readonly Color Purple = Color.FromArgb("#A45EFF");
    public static readonly Color PurpleDim = Color.FromArgb("#4A3670");

    // Stroke and Fill are Brush-typed. A Color bound into one of them doesn't get type-converted
    // the way an inline StaticResource does, so anything bound is held as a Brush from the start.
    public static readonly Brush PurpleStroke = new SolidColorBrush(Purple);
    public static readonly Brush PurpleDimStroke = new SolidColorBrush(PurpleDim);
    public static readonly Brush GreenStroke = new SolidColorBrush(Color.FromArgb("#39FF7A"));
    public static readonly Brush LineStroke = new SolidColorBrush(Color.FromArgb("#343D4D"));
}
