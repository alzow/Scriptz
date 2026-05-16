# STEP 9: Create Dashboard Page

This step creates the main dashboard with quick access to medications and profile.

## Create Directory Structure:

```bash
mkdir -p Features/Dashboard
```

## Create Features/Dashboard/DashboardPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:ScriptzApp.Features.Dashboard"
             x:Class="ScriptzApp.Features.Dashboard.DashboardPage"
             x:DataType="vm:DashboardPageViewModel"
             Title="{Binding Title}">
    
    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="20">
            
            <!-- Welcome Card -->
            <Frame BackgroundColor="{StaticResource White}"
                   HasShadow="True"
                   CornerRadius="16"
                   Padding="0">
                <Grid>
                    <BoxView Background="{StaticResource PrimaryGradient}"
                             CornerRadius="16"/>
                    <VerticalStackLayout Padding="20" Spacing="8">
                        <Label Text="{Binding WelcomeMessage}"
                               Style="{StaticResource H2}"
                               TextColor="{StaticResource White}"/>
                        <Label Text="{Binding SubMessage}"
                               Style="{StaticResource Body}"
                               TextColor="{StaticResource White}"
                               Opacity="0.9"/>
                    </VerticalStackLayout>
                </Grid>
            </Frame>

            <!-- Today's Medications -->
            <VerticalStackLayout Spacing="12">
                <Label Text="Today's Medications"
                       Style="{StaticResource H3}"
                       TextColor="{StaticResource Gray900}"/>
                
                <Frame BackgroundColor="{StaticResource Gray50}"
                       HasShadow="False"
                       BorderColor="{StaticResource Gray200}"
                       CornerRadius="12"
                       Padding="16">
                    <HorizontalStackLayout Spacing="12">
                        <Label Text="💊"
                               FontSize="32"
                               VerticalOptions="Center"/>
                        <VerticalStackLayout Spacing="4" VerticalOptions="Center">
                            <Label Text="{Binding TodayMedicationCount, StringFormat='{0} medications'}"
                                   Style="{StaticResource H3}"
                                   TextColor="{StaticResource Gray900}"/>
                            <Label Text="scheduled for today"
                                   Style="{StaticResource Caption}"/>
                        </VerticalStackLayout>
                    </HorizontalStackLayout>
                </Frame>
            </VerticalStackLayout>

            <!-- Quick Actions -->
            <VerticalStackLayout Spacing="12">
                <Label Text="Quick Actions"
                       Style="{StaticResource H3}"
                       TextColor="{StaticResource Gray900}"/>

                <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto" ColumnSpacing="12" RowSpacing="12">
                    
                    <!-- Medications -->
                    <Frame Grid.Row="0" Grid.Column="0"
                           BackgroundColor="{StaticResource White}"
                           HasShadow="True"
                           BorderColor="{StaticResource Primary}"
                           CornerRadius="16"
                           Padding="20">
                        <Frame.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding NavigateToMedicationsCommand}"/>
                        </Frame.GestureRecognizers>
                        <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                            <Label Text="💊"
                                   FontSize="48"
                                   HorizontalOptions="Center"/>
                            <Label Text="Medications"
                                   Style="{StaticResource Body}"
                                   FontAttributes="Bold"
                                   HorizontalOptions="Center"
                                   TextColor="{StaticResource Gray900}"/>
                        </VerticalStackLayout>
                    </Frame>

                    <!-- Prescriptions -->
                    <Frame Grid.Row="0" Grid.Column="1"
                           BackgroundColor="{StaticResource White}"
                           HasShadow="True"
                           BorderColor="{StaticResource Secondary}"
                           CornerRadius="16"
                           Padding="20">
                        <Frame.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding NavigateToPrescriptionsCommand}"/>
                        </Frame.GestureRecognizers>
                        <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                            <Label Text="📋"
                                   FontSize="48"
                                   HorizontalOptions="Center"/>
                            <Label Text="Prescriptions"
                                   Style="{StaticResource Body}"
                                   FontAttributes="Bold"
                                   HorizontalOptions="Center"
                                   TextColor="{StaticResource Gray900}"/>
                        </VerticalStackLayout>
                    </Frame>

                    <!-- Reminders -->
                    <Frame Grid.Row="1" Grid.Column="0"
                           BackgroundColor="{StaticResource White}"
                           HasShadow="True"
                           BorderColor="{StaticResource Tertiary}"
                           CornerRadius="16"
                           Padding="20">
                        <Frame.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding NavigateToRemindersCommand}"/>
                        </Frame.GestureRecognizers>
                        <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                            <Label Text="🔔"
                                   FontSize="48"
                                   HorizontalOptions="Center"/>
                            <Label Text="Reminders"
                                   Style="{StaticResource Body}"
                                   FontAttributes="Bold"
                                   HorizontalOptions="Center"
                                   TextColor="{StaticResource Gray900}"/>
                        </VerticalStackLayout>
                    </Frame>

                    <!-- Profile -->
                    <Frame Grid.Row="1" Grid.Column="1"
                           BackgroundColor="{StaticResource White}"
                           HasShadow="True"
                           BorderColor="{StaticResource Warning}"
                           CornerRadius="16"
                           Padding="20">
                        <Frame.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding NavigateToProfileCommand}"/>
                        </Frame.GestureRecognizers>
                        <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                            <Label Text="👤"
                                   FontSize="48"
                                   HorizontalOptions="Center"/>
                            <Label Text="Profile"
                                   Style="{StaticResource Body}"
                                   FontAttributes="Bold"
                                   HorizontalOptions="Center"
                                   TextColor="{StaticResource Gray900}"/>
                        </VerticalStackLayout>
                    </Frame>

                </Grid>
            </VerticalStackLayout>

            <!-- Logout Button -->
            <Button Text="Logout"
                    Command="{Binding LogoutCommand}"
                    BackgroundColor="Transparent"
                    BorderColor="{StaticResource Error}"
                    BorderWidth="2"
                    TextColor="{StaticResource Error}"
                    CornerRadius="12"
                    HeightRequest="50"
                    Margin="0,20,0,0"/>

        </VerticalStackLayout>
    </ScrollView>
    
</ContentPage>
```

## Create Features/Dashboard/DashboardPage.xaml.cs

```csharp
namespace ScriptzApp.Features.Dashboard;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }
}
```

## Create Features/Dashboard/DashboardPageViewModel.cs

```csharp
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using ScriptzApp.Services.Api;

namespace ScriptzApp.Features.Dashboard;

public partial class DashboardPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IScriptzPopupService _popupService;
    private readonly IApiService _apiService;

    public string WelcomeMessage { get; set; } = "Welcome to Scriptz";
    public string SubMessage { get; set; } = "Manage your medications with ease";
    public int TodayMedicationCount { get; set; } = 0;

    public DashboardPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IScriptzPopupService popupService,
        IApiService apiService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
        _apiService = apiService;
        Title = "Dashboard";
    }

    public override async Task OnLoadedAsync(NavigationParameters parameters)
    {
        await base.OnLoadedAsync(parameters);
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                // Load today's medication count
                var medications = await _apiService.Api.GetMedicationsAsync();
                TodayMedicationCount = medications?.Count(m => m.IsActive) ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                // Don't show error to user for dashboard stats
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToMedicationsAsync()
    {
        await NavigationService.NavigateAsync("MedicationsListPage");
    }

    [RelayCommand]
    private async Task NavigateToPrescriptionsAsync()
    {
        await NavigationService.NavigateAsync("PrescriptionsListPage");
    }

    [RelayCommand]
    private async Task NavigateToRemindersAsync()
    {
        await NavigationService.NavigateAsync("RemindersListPage");
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync()
    {
        await NavigationService.NavigateAsync("ProfilePage");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await _popupService.ShowConfirmAsync(
            "Logout", 
            "Are you sure you want to logout?");

        if (confirm)
        {
            await _authService.LogoutAsync();
            await NavigationService.NavigateAsync("/NavigationPage/LoginPage");
        }
    }
}
```

**STOP HERE - Confirm Dashboard page is created before proceeding to Step 10**
