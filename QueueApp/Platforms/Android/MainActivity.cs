using Android.App;
using Android.Content.PM;
using Android.OS;
using QueueApp.Framework.Theming;

namespace QueueApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // The theme's colorPrimaryDark only reaches the status bar on older Android versions, and
        // it cannot change at runtime at all, so PlatformChrome paints both bars from the live
        // theme here and again on every switch.
        PlatformChrome.Start();
    }

    // UiMode is in ConfigurationChanges above, so a system light/dark switch arrives here rather
    // than recreating the activity — the bars have to be repainted by hand.
    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        PlatformChrome.Apply(ThemePalette.Current);
    }
}
