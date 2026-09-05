using Android.App;
using Android.Content.PM;
using Android.OS;
using QueueApp.Constants;
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

        CreateNotificationChannels();
    }

    // Idempotent and safe to call on every launch — it does not reset a user's own channel
    // overrides. Channels are an API 26+ concept; below that, FCM shows notifications without one.
    private void CreateNotificationChannels()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var channel = new NotificationChannel(
            NotificationChannels.QueueUpdatesId,
            NotificationChannels.QueueUpdatesName,
            NotificationImportance.High)
        {
            Description = NotificationChannels.QueueUpdatesDescription,
            LockscreenVisibility = NotificationVisibility.Public,
        };

        channel.EnableVibration(true);
        channel.SetShowBadge(true);

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    // UiMode is in ConfigurationChanges above, so a system light/dark switch arrives here rather
    // than recreating the activity — the bars have to be repainted by hand.
    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        PlatformChrome.Apply(ThemePalette.Current);
    }
}
