using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MPowerKit.Navigation;
using MPowerKit.Popups;
using SkiaSharp.Views.Maui.Controls.Hosting;

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
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
