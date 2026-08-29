namespace QueueApp.Framework.Theming;

public enum ThemeChoice
{
    /// <summary>Follow the phone. The default, and what a phone's owner expects.</summary>
    System,
    Light,
    Dark,
}

/// <summary>
/// Owns the app's theme: what the operator picked, where it is remembered, and pushing it onto
/// <see cref="Application.UserAppTheme"/>.
///
/// Static rather than injected because it has to run before the container exists — the choice is
/// applied in the App constructor, ahead of the first page, or the app paints one theme and then
/// visibly flips to the other.
/// </summary>
public static class ThemeService
{
    private const string PreferenceKey = "app_theme_choice";

    /// <summary>Raised after the effective theme changes, whether by choice or by the system.</summary>
    public static event EventHandler<AppTheme>? ThemeChanged;

    public static ThemeChoice Current { get; private set; } = ThemeChoice.System;

    /// <summary>The theme actually on screen, with System resolved against the phone's setting.</summary>
    public static AppTheme Effective => ThemePalette.Current;

    /// <summary>
    /// Read the stored choice and apply it. Call from the App constructor, before the first page is
    /// created.
    /// </summary>
    public static void Initialise(Application app)
    {
        Current = Load();
        Apply(app, Current);

        // A system-level switch only matters while we are following the system, but the platform
        // chrome has to be repainted either way, so the handler always runs.
        app.RequestedThemeChanged += (_, _) => OnSystemThemeChanged(app);
    }

    /// <summary>Change the choice, persist it, and repaint.</summary>
    public static void Set(ThemeChoice choice)
    {
        Current = choice;
        Save(choice);

        var app = Application.Current;
        if (app is null)
            return;

        Apply(app, choice);
        ThemeChanged?.Invoke(null, Effective);
    }

    private static void OnSystemThemeChanged(Application app)
    {
        if (Current == ThemeChoice.System)
            Apply(app, ThemeChoice.System);

        ThemeChanged?.Invoke(null, Effective);
    }

    private static void Apply(Application app, ThemeChoice choice) =>
        app.UserAppTheme = choice switch
        {
            ThemeChoice.Light => AppTheme.Light,
            ThemeChoice.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };

    private static ThemeChoice Load()
    {
        try
        {
            var stored = Preferences.Default.Get(PreferenceKey, nameof(ThemeChoice.System));
            return Enum.TryParse<ThemeChoice>(stored, out var choice) ? choice : ThemeChoice.System;
        }
        catch (Exception)
        {
            // Preferences can throw on a locked device before first unlock. Following the system is
            // the right answer when we cannot read the choice.
            return ThemeChoice.System;
        }
    }

    private static void Save(ThemeChoice choice)
    {
        try
        {
            Preferences.Default.Set(PreferenceKey, choice.ToString());
        }
        catch (Exception)
        {
        }
    }
}
