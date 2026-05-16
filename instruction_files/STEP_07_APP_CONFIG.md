# STEP 7: Update MauiProgram.cs and App.xaml

This step updates the main application configuration files.

## Replace MauiProgram.cs contents

```csharp
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MPowerKit.Navigation;
using MPowerKit.Popups;
using ScriptzApp.Services.Api;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using CommunityToolkit.Mvvm.Messaging;

namespace ScriptzApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMPowerKitNavigation()
            .UseMPowerKitPopups()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register Core Services
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IScriptzPopupService, ScriptzPopupService>();

        // Register Auth Service
        builder.Services.AddSingleton<IAuthService, AuthService>();

        // Configure Refit API
        builder.Services.ConfigureRefitApi();

        // Auto-register Pages and ViewModels
        builder.Services.RegisterPagesAndViewModels();

        return builder.Build();
    }
}
```

## Replace App.xaml contents

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="ScriptzApp.App">
    <Application.Resources>
        <ResourceDictionary>
            
            <!-- Colors -->
            <Color x:Key="Primary">#6366F1</Color>
            <Color x:Key="PrimaryDark">#4F46E5</Color>
            <Color x:Key="Secondary">#EC4899</Color>
            <Color x:Key="SecondaryDark">#DB2777</Color>
            <Color x:Key="Tertiary">#14B8A6</Color>
            <Color x:Key="Success">#10B981</Color>
            <Color x:Key="Warning">#F59E0B</Color>
            <Color x:Key="Error">#EF4444</Color>
            
            <Color x:Key="White">#FFFFFF</Color>
            <Color x:Key="Black">#000000</Color>
            <Color x:Key="Gray50">#F9FAFB</Color>
            <Color x:Key="Gray100">#F3F4F6</Color>
            <Color x:Key="Gray200">#E5E7EB</Color>
            <Color x:Key="Gray300">#D1D5DB</Color>
            <Color x:Key="Gray400">#9CA3AF</Color>
            <Color x:Key="Gray500">#6B7280</Color>
            <Color x:Key="Gray600">#4B5563</Color>
            <Color x:Key="Gray700">#374151</Color>
            <Color x:Key="Gray800">#1F2937</Color>
            <Color x:Key="Gray900">#111827</Color>

            <!-- Gradients -->
            <LinearGradientBrush x:Key="PrimaryGradient" StartPoint="0,0" EndPoint="1,1">
                <GradientStop Color="{StaticResource Primary}" Offset="0.0" />
                <GradientStop Color="{StaticResource PrimaryDark}" Offset="1.0" />
            </LinearGradientBrush>

            <LinearGradientBrush x:Key="SecondaryGradient" StartPoint="0,0" EndPoint="1,1">
                <GradientStop Color="{StaticResource Secondary}" Offset="0.0" />
                <GradientStop Color="{StaticResource SecondaryDark}" Offset="1.0" />
            </LinearGradientBrush>

            <!-- Styles -->
            <Style TargetType="NavigationPage">
                <Setter Property="BarBackgroundColor" Value="{StaticResource Primary}"/>
                <Setter Property="BarTextColor" Value="{StaticResource White}"/>
            </Style>

            <Style TargetType="Label" x:Key="H1">
                <Setter Property="FontSize" Value="32"/>
                <Setter Property="FontAttributes" Value="Bold"/>
            </Style>

            <Style TargetType="Label" x:Key="H2">
                <Setter Property="FontSize" Value="24"/>
                <Setter Property="FontAttributes" Value="Bold"/>
            </Style>

            <Style TargetType="Label" x:Key="H3">
                <Setter Property="FontSize" Value="20"/>
                <Setter Property="FontAttributes" Value="Bold"/>
            </Style>

            <Style TargetType="Label" x:Key="Body">
                <Setter Property="FontSize" Value="16"/>
            </Style>

            <Style TargetType="Label" x:Key="Caption">
                <Setter Property="FontSize" Value="14"/>
                <Setter Property="TextColor" Value="{StaticResource Gray500}"/>
            </Style>

        </ResourceDictionary>
    </Application.Resources>
</Application>
```

## Replace App.xaml.cs contents

```csharp
using MPowerKit.Navigation;

namespace ScriptzApp;

public partial class App : Application
{
    public App(INavigationService navigationService)
    {
        InitializeComponent();

        // Configure initial navigation
        NavigationStartup.Configure(navigationService);
    }
}
```

**STOP HERE - Confirm all app configuration files are updated before proceeding to Step 8**
