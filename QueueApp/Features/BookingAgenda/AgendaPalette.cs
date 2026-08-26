namespace QueueApp.Features.BookingAgenda;

// The same values as Resources/Styles/Colors.xaml, in code because every row's colours are worked
// out once at map time rather than by a converter running per binding on a recycled cell (spec §11).
// If Colors.xaml moves, these move with it.
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

    public static readonly Color Ink = Color.FromArgb("#E2E9F5");
    public static readonly Color Muted = Color.FromArgb("#7C8899");
    public static readonly Color Dim = Color.FromArgb("#5D6879");
    public static readonly Color OnGreen = Color.FromArgb("#0E1219");

    public static readonly Color Transparent = Colors.Transparent;
}
