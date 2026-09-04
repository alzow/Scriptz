namespace QueueApp.Framework.Theming;

/// <summary>
/// Resolves a semantic design token to the raw value for the theme that is currently showing.
///
/// XAML gets this for free through AppThemeBinding. Code does not, so anything that builds a row
/// model or paints a control from C# comes through here instead of naming a colour: the token names
/// match the ones in Colors.xaml, minus the Dark/Light prefix.
///
/// Every accessor is a property rather than a cached field. A cached field would freeze whichever
/// theme happened to be active when the type was first touched, and the app would keep painting
/// yesterday's palette after a switch.
/// </summary>
public static class ThemePalette
{
    public static AppTheme Current
    {
        get
        {
            var app = Application.Current;
            if (app is null)
                return AppTheme.Dark;

            // UserAppTheme wins when the operator has pinned a theme; otherwise follow the system.
            return app.UserAppTheme switch
            {
                AppTheme.Light => AppTheme.Light,
                AppTheme.Dark => AppTheme.Dark,
                _ => app.RequestedTheme == AppTheme.Light ? AppTheme.Light : AppTheme.Dark,
            };
        }
    }

    public static bool IsLight => Current == AppTheme.Light;

    public static Color Get(string token)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
            return Colors.Transparent;

        var key = (IsLight ? "Light" : "Dark") + token;
        return resources.TryGetValue(key, out var value) && value is Color color
            ? color
            : Colors.Transparent;
    }

    public static Brush Brush(string token) => new SolidColorBrush(Get(token));

    public static Color Bg => Get("Bg");
    public static Color Surface => Get("Surface");
    public static Color Raised => Get("Raised");
    public static Color Border => Get("Border");

    public static Color TextInk => Get("TextInk");
    public static Color TextMuted => Get("TextMuted");
    public static Color TextDim => Get("TextDim");
    public static Color TextOnAccent => Get("TextOnAccent");

    public static Color Accent => Get("Accent");
    public static Color Purple => Get("Purple");
    public static Color Danger => Get("Danger");

    public static Color AccentText => Get("AccentText");
    public static Color PurpleText => Get("PurpleText");
    public static Color DangerText => Get("DangerText");

    public static Color AccentTint => Get("AccentTint");
    public static Color PurpleTint => Get("PurpleTint");
    public static Color DangerTint => Get("DangerTint");
    public static Color AccentBorder => Get("AccentBorder");
    public static Color PurpleBorder => Get("PurpleBorder");
    public static Color DangerBorder => Get("DangerBorder");

    public static Color Scrim => Get("Scrim");
}
