namespace QueueApp.Features.BookingAgenda.Models;

public static class AgendaPalette
{
    public static readonly Color Surface = Color.FromArgb("#1C222D");
    public static readonly Color SurfaceRaised = Color.FromArgb("#252C39");
    public static readonly Color Line = Color.FromArgb("#343D4D");

    public static readonly Color Green = Color.FromArgb("#39FF7A");
    public static readonly Color GreenBorder = Color.FromArgb("#2E5F42");
    public static readonly Color GreenTint = Color.FromArgb("#0D39FF7A");

    public static readonly Color Purple = Color.FromArgb("#A45EFF");
    public static readonly Color PurpleBorder = Color.FromArgb("#4A3670");
    public static readonly Color PurpleTint = Color.FromArgb("#29A45EFF");

    public static readonly Color Ink = Color.FromArgb("#F2F4F7");
    public static readonly Color Muted = Color.FromArgb("#8A8F98");
    public static readonly Color Dim = Color.FromArgb("#565C68");
    public static readonly Color OnGreen = Color.FromArgb("#141821");

    public static readonly Brush PurpleStroke = new SolidColorBrush(Purple);
    public static readonly Brush PurpleDimStroke = new SolidColorBrush(PurpleBorder);
}
