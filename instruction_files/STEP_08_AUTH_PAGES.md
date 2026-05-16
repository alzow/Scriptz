# STEP 8: Create Authentication Pages (Login & Register)

This step creates the login and registration UI and logic.

## Create Directory Structure:

```bash
mkdir -p Features/Auth
```

## Create Features/Auth/LoginPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:ScriptzApp.Features.Auth"
             x:Class="ScriptzApp.Features.Auth.LoginPage"
             x:DataType="vm:LoginPageViewModel"
             Shell.NavBarIsVisible="False">
    
    <Grid>
        <ScrollView>
            <VerticalStackLayout Padding="30" Spacing="25" VerticalOptions="Center">
                
                <!-- Logo Section -->
                <VerticalStackLayout Spacing="10" Margin="0,40,0,40">
                    <Label Text="💊"
                           FontSize="64"
                           HorizontalOptions="Center"/>
                    <Label Text="Scriptz"
                           Style="{StaticResource H1}"
                           HorizontalOptions="Center"
                           TextColor="{StaticResource Primary}"/>
                    <Label Text="Your medication companion"
                           Style="{StaticResource Caption}"
                           HorizontalOptions="Center"/>
                </VerticalStackLayout>

                <Label Text="Welcome Back"
                       Style="{StaticResource H2}"
                       HorizontalOptions="Center"
                       Margin="0,0,0,20"/>

                <!-- Email Entry -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    
                    <Entry Placeholder="Email address"
                           Text="{Binding Email}"
                           Keyboard="Email"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Password Entry -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    
                    <Entry Placeholder="Password"
                           Text="{Binding Password}"
                           IsPassword="True"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Login Button -->
                <Button Text="Login"
                        Command="{Binding LoginCommand}"
                        Background="{StaticResource PrimaryGradient}"
                        TextColor="{StaticResource White}"
                        FontAttributes="Bold"
                        FontSize="18"
                        CornerRadius="12"
                        HeightRequest="55"
                        Margin="0,10,0,0"/>

                <!-- Register Link -->
                <HorizontalStackLayout HorizontalOptions="Center" Spacing="5" Margin="0,20,0,0">
                    <Label Text="Don't have an account?"
                           Style="{StaticResource Body}"
                           TextColor="{StaticResource Gray600}"
                           VerticalOptions="Center"/>
                    <Label Text="Register"
                           Style="{StaticResource Body}"
                           TextColor="{StaticResource Primary}"
                           FontAttributes="Bold"
                           VerticalOptions="Center">
                        <Label.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding NavigateToRegisterCommand}"/>
                        </Label.GestureRecognizers>
                    </Label>
                </HorizontalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <!-- Loading Overlay -->
        <Grid IsVisible="{Binding IsBusy}" BackgroundColor="#80000000">
            <ActivityIndicator IsRunning="{Binding IsBusy}"
                              Color="{StaticResource Primary}"
                              VerticalOptions="Center"
                              HorizontalOptions="Center"
                              Scale="1.5"/>
        </Grid>
    </Grid>
    
</ContentPage>
```

## Create Features/Auth/LoginPage.xaml.cs

```csharp
namespace ScriptzApp.Features.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }
}
```

## Create Features/Auth/LoginPageViewModel.cs

```csharp
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;

namespace ScriptzApp.Features.Auth;

public partial class LoginPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IScriptzPopupService _popupService;

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public LoginPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
        Title = "Login";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await _popupService.ShowAlertAsync("Validation Error", "Please enter email and password");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new LoginRequest
            {
                Email = Email.Trim(),
                Password = Password
            };

            var result = await _authService.LoginAsync(request);

            if (result != null)
            {
                // Navigate to dashboard
                await NavigationService.NavigateAsync("/NavigationPage/DashboardPage");
            }
            else
            {
                await _popupService.ShowAlertAsync("Login Failed", "Invalid email or password");
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await NavigationService.NavigateAsync("RegisterPage");
    }
}
```

## Create Features/Auth/RegisterPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:ScriptzApp.Features.Auth"
             x:Class="ScriptzApp.Features.Auth.RegisterPage"
             x:DataType="vm:RegisterPageViewModel"
             Title="Create Account">
    
    <Grid>
        <ScrollView>
            <VerticalStackLayout Padding="30" Spacing="20">
                
                <Label Text="Create Account"
                       Style="{StaticResource H2}"
                       HorizontalOptions="Center"
                       Margin="0,20,0,30"/>

                <!-- First Name -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    <Entry Placeholder="First Name"
                           Text="{Binding FirstName}"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Last Name -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    <Entry Placeholder="Last Name"
                           Text="{Binding LastName}"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Phone Number -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    <Entry Placeholder="Phone Number"
                           Text="{Binding PhoneNumber}"
                           Keyboard="Telephone"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Email -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    <Entry Placeholder="Email"
                           Text="{Binding Email}"
                           Keyboard="Email"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Password -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    <Entry Placeholder="Password"
                           Text="{Binding Password}"
                           IsPassword="True"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Confirm Password -->
                <Border BackgroundColor="{StaticResource Gray50}"
                        Stroke="{StaticResource Gray300}"
                        StrokeThickness="1"
                        Padding="15,10">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="12"/>
                    </Border.StrokeShape>
                    <Entry Placeholder="Confirm Password"
                           Text="{Binding ConfirmPassword}"
                           IsPassword="True"
                           TextColor="{StaticResource Gray900}"
                           PlaceholderColor="{StaticResource Gray400}"/>
                </Border>

                <!-- Register Button -->
                <Button Text="Create Account"
                        Command="{Binding RegisterCommand}"
                        Background="{StaticResource PrimaryGradient}"
                        TextColor="{StaticResource White}"
                        FontAttributes="Bold"
                        FontSize="18"
                        CornerRadius="12"
                        HeightRequest="55"
                        Margin="0,20,0,0"/>

                <!-- Login Link -->
                <HorizontalStackLayout HorizontalOptions="Center" Spacing="5" Margin="0,10,0,20">
                    <Label Text="Already have an account?"
                           Style="{StaticResource Body}"
                           TextColor="{StaticResource Gray600}"
                           VerticalOptions="Center"/>
                    <Label Text="Login"
                           Style="{StaticResource Body}"
                           TextColor="{StaticResource Primary}"
                           FontAttributes="Bold"
                           VerticalOptions="Center">
                        <Label.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding NavigateToLoginCommand}"/>
                        </Label.GestureRecognizers>
                    </Label>
                </HorizontalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <!-- Loading Overlay -->
        <Grid IsVisible="{Binding IsBusy}" BackgroundColor="#80000000">
            <ActivityIndicator IsRunning="{Binding IsBusy}"
                              Color="{StaticResource Primary}"
                              VerticalOptions="Center"
                              HorizontalOptions="Center"
                              Scale="1.5"/>
        </Grid>
    </Grid>
    
</ContentPage>
```

## Create Features/Auth/RegisterPage.xaml.cs

```csharp
namespace ScriptzApp.Features.Auth;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }
}
```

## Create Features/Auth/RegisterPageViewModel.cs

```csharp
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;

namespace ScriptzApp.Features.Auth;

public partial class RegisterPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IScriptzPopupService _popupService;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public RegisterPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
        Title = "Register";
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) ||
            string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await _popupService.ShowAlertAsync("Validation Error", "Please fill in all required fields");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await _popupService.ShowAlertAsync("Validation Error", "Passwords do not match");
            return;
        }

        if (Password.Length < 6)
        {
            await _popupService.ShowAlertAsync("Validation Error", "Password must be at least 6 characters");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new RegisterRequest
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Email = Email.Trim(),
                Password = Password,
                ConfirmPassword = ConfirmPassword
            };

            var result = await _authService.RegisterAsync(request);

            if (result != null)
            {
                await _popupService.ShowAlertAsync("Success", "Account created successfully!");
                await NavigationService.NavigateAsync("/NavigationPage/DashboardPage");
            }
            else
            {
                await _popupService.ShowAlertAsync("Registration Failed", "Unable to create account. Please try again.");
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
```

**STOP HERE - Confirm all authentication pages are created before proceeding to Step 9**
