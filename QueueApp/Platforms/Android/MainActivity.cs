using Android.App;
using Android.Content.PM;
using Android.OS;

namespace QueueApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // DeepBackground from Resources/Styles/Colors.xaml. The theme's colorPrimaryDark only reaches
    // the status bar on older Android versions, so the bars are set here as well to keep the
    // system chrome the same colour as the page behind it on every version.
    private static readonly Android.Graphics.Color SystemBarColor =
        Android.Graphics.Color.ParseColor("#141821");

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Window is null)
            return;

        Window.SetStatusBarColor(SystemBarColor);
        Window.SetNavigationBarColor(SystemBarColor);
    }
}
