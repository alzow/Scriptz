using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using MPowerKit.Popups;
using SkiaSharp.Views.Maui.Controls.Hosting;
#if ANDROID
using Android.Graphics.Drawables;
using Plugin.Firebase.Core.Platforms.Android;
#elif IOS
using UIKit;
#endif

namespace QueueApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMPowerKitNavigation(NavigationStartup.Configure)
            .UseMPowerKitPopups()
            .UseSkiaSharp()
            .RegisterFirebaseServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Roboto-Thin.ttf", "RobotoThin");
                fonts.AddFont("Roboto-ThinItalic.ttf", "RobotoThinItalic");
                fonts.AddFont("Roboto-Light.ttf", "RobotoLight");
                fonts.AddFont("Roboto-LightItalic.ttf", "RobotoLightItalic");
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Italic.ttf", "RobotoItalic");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
                fonts.AddFont("Roboto-MediumItalic.ttf", "RobotoMediumItalic");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
                fonts.AddFont("Roboto-BoldItalic.ttf", "RobotoBoldItalic");
                fonts.AddFont("Roboto-Black.ttf", "RobotoBlack");
                fonts.AddFont("Roboto-BlackItalic.ttf", "RobotoBlackItalic");
            })
            .ConfigureMauiHandlers(handlers =>
            {
                EntryHandler.Mapper.AppendToMapping("RemoveNativeBorder", (handler, view) =>
                {
#if ANDROID
                    handler.PlatformView.Background = new ColorDrawable(Android.Graphics.Color.Transparent);
                    handler.PlatformView.SetPadding(0, handler.PlatformView.PaddingTop, 0, handler.PlatformView.PaddingBottom);
#elif IOS
                    handler.PlatformView.BorderStyle = UITextBorderStyle.None;
#endif
                });
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

#if ANDROID
        var pushRegistration = app.Services.GetRequiredService<Services.Auth.IPushRegistrationService>();
        Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.TokenChanged += pushRegistration.OnTokenRefreshed;
#endif

        // Deliberately not Android-only: the tap event and its payload are the same on both
        // platforms, so this routes iOS taps unchanged the day the iOS Firebase setup lands.
        // Subscribing here rather than in a page is what makes a cold start work — the plugin
        // replays the tap that launched the app to the first subscriber it gets.
#if ANDROID || IOS
        var pushRouter = app.Services.GetRequiredService<Services.Notifications.IPushNotificationRouter>();
        Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.NotificationTapped +=
            (_, e) => pushRouter.OnNotificationTapped(e.Notification?.Data);
#endif

        return app;
    }

    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
                Plugin.Firebase.Core.Platforms.Android.CrossFirebase.Initialize(
                    activity,
                    () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity)));
#endif
        });

        return builder;
    }
}
