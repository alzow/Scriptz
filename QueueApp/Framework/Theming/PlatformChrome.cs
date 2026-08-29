#if ANDROID
using AndroidX.Core.View;
#elif IOS
using UIKit;
#endif

namespace QueueApp.Framework.Theming;

/// <summary>
/// The system bars around the app. Easy to forget and very visible when wrong: a dark status bar
/// over a light page reads as a rendering bug, and dark icons on a dark bar are simply invisible.
///
/// Android's theme XML can only carry one colour, so the bars are painted here on every theme
/// change as well as at startup.
/// </summary>
public static class PlatformChrome
{
    private static bool _subscribed;

    /// <summary>Paint the chrome now, and keep it in step with the theme from here on.</summary>
    public static void Start()
    {
        Apply(ThemePalette.Current);

        if (_subscribed)
            return;

        ThemeService.ThemeChanged += (_, theme) =>
            MainThread.BeginInvokeOnMainThread(() => Apply(theme));
        _subscribed = true;
    }

    public static void Apply(AppTheme theme)
    {
        var isLight = theme == AppTheme.Light;

        try
        {
#if ANDROID
            var activity = Platform.CurrentActivity;
            var window = activity?.Window;
            if (window is null)
                return;

            // The bars match the page behind them, so the chrome reads as part of the app rather
            // than a frame around it.
            var bar = ToPlatform(ThemePalette.Bg);
            window.SetStatusBarColor(bar);
            window.SetNavigationBarColor(bar);

            // Light bars need dark icons and vice versa; without this the icons vanish on light.
            var controller = WindowCompat.GetInsetsController(window, window.DecorView);
            if (controller is not null)
            {
                controller.AppearanceLightStatusBars = isLight;
                controller.AppearanceLightNavigationBars = isLight;
            }
#elif IOS
            // MAUI maps UserAppTheme onto the window's UserInterfaceStyle, and the status bar
            // follows it. Setting it explicitly keeps the two in step when the app is launched
            // straight into a pinned theme that differs from the system one.
            var style = isLight ? UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;
            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is not UIWindowScene windowScene)
                    continue;

                foreach (var window in windowScene.Windows)
                    window.OverrideUserInterfaceStyle = style;
            }
#endif
        }
        catch (Exception)
        {
            // Chrome is cosmetic. A platform that will not hand us a window is not worth a crash.
        }
    }

#if ANDROID
    private static Android.Graphics.Color ToPlatform(Color color) =>
        new((byte)(color.Red * 255), (byte)(color.Green * 255), (byte)(color.Blue * 255), (byte)(color.Alpha * 255));
#endif
}
