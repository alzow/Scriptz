namespace QueueApp.Framework.Theming;

/// <summary>
/// Icon assets are rasterised from SVG at build time, so a single file cannot follow a runtime
/// theme switch the way an AppThemeBinding can. Each themeable icon therefore ships twice —
/// "ic_close" and "ic_close_light" — and this picks between them.
///
/// The light twins are the same artwork with the ink swapped off the token table: #E2E9F5 becomes
/// #161C27, and #39FF7A becomes #097430 because the brand green as a 4px stroke on a light page is
/// 1.22:1 and simply is not there.
/// </summary>
public static class ThemedIcons
{
    public const string LightSuffix = "_light";

    /// <summary>Resolve a base icon name to the file for whichever theme is showing.</summary>
    public static ImageSource? Resolve(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return ImageSource.FromFile(ThemePalette.IsLight ? name + LightSuffix : name);
    }
}
