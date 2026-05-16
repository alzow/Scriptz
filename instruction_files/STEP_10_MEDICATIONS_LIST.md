# STEP 10: Create Medications List Page (Scriptz UI Design)

This step creates the medications list page with the warm cream + burnt orange design from the Scriptz prototype.

## Create Directory Structure:

```bash
mkdir -p Features/Medications
```

## Create Features/Medications/MedicationsListPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:ScriptzApp.Features.Medications"
             x:Class="ScriptzApp.Features.Medications.MedicationsListPage"
             x:DataType="vm:MedicationsListPageViewModel"
             Title="My Medications">
    
    <Grid RowDefinitions="Auto,*,Auto" BackgroundColor="{StaticResource Gray50}">
        
        <!-- Header Section -->
        <VerticalStackLayout Grid.Row="0" Padding="20" Spacing="12" BackgroundColor="{StaticResource White}">
            <Label Text="My Medications"
                   Style="{StaticResource H2}"
                   TextColor="{StaticResource Gray900}"/>
            
            <!-- Stats Row -->
            <Grid ColumnDefinitions="*,*,*" ColumnSpacing="8">
                <!-- Active -->
                <Frame Grid.Column="0" 
                       BackgroundColor="{StaticResource Success}"
                       Padding="12,8"
                       CornerRadius="8"
                       HasShadow="False">
                    <VerticalStackLayout Spacing="2">
                        <Label Text="{Binding ActiveCount}"
                               FontSize="24"
                               FontAttributes="Bold"
                               TextColor="{StaticResource White}"
                               HorizontalOptions="Center"/>
                        <Label Text="Active"
                               FontSize="11"
                               TextColor="{StaticResource White}"
                               Opacity="0.9"
                               HorizontalOptions="Center"/>
                    </VerticalStackLayout>
                </Frame>

                <!-- Chronic -->
                <Frame Grid.Column="1" 
                       BackgroundColor="{StaticResource Primary}"
                       Padding="12,8"
                       CornerRadius="8"
                       HasShadow="False">
                    <VerticalStackLayout Spacing="2">
                        <Label Text="{Binding ChronicCount}"
                               FontSize="24"
                               FontAttributes="Bold"
                               TextColor="{StaticResource White}"
                               HorizontalOptions="Center"/>
                        <Label Text="Chronic"
                               FontSize="11"
                               TextColor="{StaticResource White}"
                               Opacity="0.9"
                               HorizontalOptions="Center"/>
                    </VerticalStackLayout>
                </Frame>

                <!-- Refills Due -->
                <Frame Grid.Column="2" 
                       BackgroundColor="{StaticResource Warning}"
                       Padding="12,8"
                       CornerRadius="8"
                       HasShadow="False">
                    <VerticalStackLayout Spacing="2">
                        <Label Text="{Binding RefillsDueCount}"
                               FontSize="24"
                               FontAttributes="Bold"
                               TextColor="{StaticResource White}"
                               HorizontalOptions="Center"/>
                        <Label Text="Due Soon"
                               FontSize="11"
                               TextColor="{StaticResource White}"
                               Opacity="0.9"
                               HorizontalOptions="Center"/>
                    </VerticalStackLayout>
                </Frame>
            </Grid>
        </VerticalStackLayout>

        <!-- Medications List -->
        <RefreshView Grid.Row="1" 
                     IsRefreshing="{Binding IsRefreshing}"
                     Command="{Binding RefreshCommand}">
            <CollectionView ItemsSource="{Binding Medications}"
                           SelectionMode="None"
                           Margin="0,12,0,0">
                <CollectionView.EmptyView>
                    <VerticalStackLayout Padding="40" Spacing="16" VerticalOptions="Center">
                        <Label Text="💊"
                               FontSize="64"
                               HorizontalOptions="Center"/>
                        <Label Text="No medications yet"
                               Style="{StaticResource H3}"
                               HorizontalOptions="Center"
                               TextColor="{StaticResource Gray600}"/>
                        <Label Text="Add your first medication to get started"
                               Style="{StaticResource Caption}"
                               HorizontalOptions="Center"/>
                    </VerticalStackLayout>
                </CollectionView.EmptyView>

                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Frame Margin="16,0,16,12"
                               Padding="0"
                               CornerRadius="12"
                               HasShadow="True"
                               BackgroundColor="{StaticResource White}">
                            <Frame.GestureRecognizers>
                                <TapGestureRecognizer 
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:MedicationsListPageViewModel}}, Path=ViewMedicationCommand}"
                                    CommandParameter="{Binding .}"/>
                            </Frame.GestureRecognizers>

                            <Grid ColumnDefinitions="Auto,*,Auto" Padding="16" ColumnSpacing="12">
                                
                                <!-- Icon -->
                                <Frame Grid.Column="0"
                                       WidthRequest="48"
                                       HeightRequest="48"
                                       CornerRadius="24"
                                       Padding="0"
                                       HasShadow="False"
                                       VerticalOptions="Start"
                                       BackgroundColor="{StaticResource Gray100}">
                                    <Label Text="💊"
                                           FontSize="24"
                                           HorizontalOptions="Center"
                                           VerticalOptions="Center"/>
                                </Frame>

                                <!-- Details -->
                                <VerticalStackLayout Grid.Column="1" Spacing="6" VerticalOptions="Center">
                                    <Label Text="{Binding Name}"
                                           Style="{StaticResource Body}"
                                           FontAttributes="Bold"
                                           TextColor="{StaticResource Gray900}"/>
                                    
                                    <Label Text="{Binding GenericName}"
                                           Style="{StaticResource Caption}"
                                           TextColor="{StaticResource Gray500}"
                                           IsVisible="{Binding GenericName, Converter={StaticResource IsNotNullOrEmptyConverter}}"/>
                                    
                                    <HorizontalStackLayout Spacing="4">
                                        <Label Text="{Binding Dosage}"
                                               Style="{StaticResource Caption}"
                                               TextColor="{StaticResource Gray600}"/>
                                        <Label Text="·"
                                               Style="{StaticResource Caption}"
                                               TextColor="{StaticResource Gray400}"/>
                                        <Label Text="{Binding Form}"
                                               Style="{StaticResource Caption}"
                                               TextColor="{StaticResource Gray600}"/>
                                    </HorizontalStackLayout>

                                    <Label Text="{Binding Frequency}"
                                           Style="{StaticResource Caption}"
                                           TextColor="{StaticResource Primary}"/>
                                </VerticalStackLayout>

                                <!-- Status Badge -->
                                <VerticalStackLayout Grid.Column="2" Spacing="6" VerticalOptions="Start">
                                    <Frame Padding="8,4"
                                           CornerRadius="6"
                                           HasShadow="False"
                                           BackgroundColor="{Binding IsActive, Converter={StaticResource BoolToColorConverter}, ConverterParameter='{StaticResource Success}|{StaticResource Gray300}'}">
                                        <Label Text="{Binding IsActive, Converter={StaticResource BoolToTextConverter}, ConverterParameter='Active|Inactive'}"
                                               FontSize="11"
                                               FontAttributes="Bold"
                                               TextColor="{StaticResource White}"/>
                                    </Frame>

                                    <!-- Next refill indicator -->
                                    <Label Text="{Binding DaysUntilRefill, StringFormat='Due in {0}d'}"
                                           FontSize="11"
                                           TextColor="{StaticResource Warning}"
                                           FontAttributes="Bold"
                                           IsVisible="{Binding ShowRefillWarning}"/>
                                </VerticalStackLayout>

                            </Grid>
                        </Frame>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </RefreshView>

        <!-- Add Button -->
        <Grid Grid.Row="2" Padding="20" BackgroundColor="{StaticResource White}">
            <Button Text="➕ Add New Medication"
                    Command="{Binding AddMedicationCommand}"
                    Background="{StaticResource PrimaryGradient}"
                    TextColor="{StaticResource White}"
                    FontAttributes="Bold"
                    FontSize="16"
                    CornerRadius="12"
                    HeightRequest="50"/>
        </Grid>

        <!-- Loading Overlay -->
        <Grid Grid.Row="0" Grid.RowSpan="3" 
              IsVisible="{Binding IsBusy}" 
              BackgroundColor="#80000000">
            <ActivityIndicator IsRunning="{Binding IsBusy}"
                              Color="{StaticResource Primary}"
                              VerticalOptions="Center"
                              HorizontalOptions="Center"
                              Scale="1.5"/>
        </Grid>
        
    </Grid>
    
</ContentPage>
```

## Create Features/Medications/MedicationsListPage.xaml.cs

```csharp
namespace ScriptzApp.Features.Medications;

public partial class MedicationsListPage : ContentPage
{
    public MedicationsListPage()
    {
        InitializeComponent();
    }
}
```

## Create Features/Medications/MedicationsListPageViewModel.cs

```csharp
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Models.Api.Responses;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using ScriptzApp.Services.Api;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Medications;

public partial class MedicationsListPageViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IScriptzPopupService _popupService;

    public ObservableCollection<MedicationDisplayModel> Medications { get; set; } = new();
    
    public int ActiveCount { get; set; }
    public int ChronicCount { get; set; }
    public int RefillsDueCount { get; set; }
    public bool IsRefreshing { get; set; }

    public MedicationsListPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IApiService apiService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _apiService = apiService;
        _popupService = popupService;
        Title = "My Medications";
    }

    public override async Task OnLoadedAsync(NavigationParameters parameters)
    {
        await base.OnLoadedAsync(parameters);
        await LoadMedicationsAsync();
    }

    [RelayCommand]
    private async Task LoadMedicationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                var medications = await _apiService.Api.GetMedicationsAsync();
                
                Medications.Clear();
                
                if (medications != null)
                {
                    foreach (var med in medications.OrderBy(m => m.Name))
                    {
                        Medications.Add(new MedicationDisplayModel(med));
                    }

                    // Calculate stats
                    ActiveCount = medications.Count(m => m.IsActive);
                    ChronicCount = medications.Count(m => m.IsActive && m.Frequency.Contains("daily", StringComparison.OrdinalIgnoreCase));
                    RefillsDueCount = Medications.Count(m => m.ShowRefillWarning);
                }
            }
            catch (Exception ex)
            {
                await _popupService.ShowAlertAsync("Error", "Failed to load medications. Please try again.");
                System.Diagnostics.Debug.WriteLine($"Error loading medications: {ex.Message}");
            }
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadMedicationsAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task AddMedicationAsync()
    {
        await NavigationService.NavigateAsync("MedicationDetailPage");
    }

    [RelayCommand]
    private async Task ViewMedicationAsync(MedicationDisplayModel medication)
    {
        var parameters = new NavigationParameters
        {
            { "MedicationId", medication.Id }
        };
        
        await NavigationService.NavigateAsync("MedicationDetailPage", parameters);
    }
}

// Display model with calculated properties
public class MedicationDisplayModel : MedicationResponse
{
    public MedicationDisplayModel(MedicationResponse response)
    {
        Id = response.Id;
        Name = response.Name;
        GenericName = response.GenericName;
        Dosage = response.Dosage;
        Form = response.Form;
        Frequency = response.Frequency;
        Instructions = response.Instructions;
        StartDate = response.StartDate;
        EndDate = response.EndDate;
        IsActive = response.IsActive;
        CreatedAt = response.CreatedAt;
        UpdatedAt = response.UpdatedAt;
    }

    public int DaysUntilRefill
    {
        get
        {
            // Simple calculation - assume 30 day supply
            // In real app, this would be based on actual refill schedule
            var daysSinceStart = (DateTime.Now - StartDate).Days;
            var daysInCycle = 30;
            var daysUntilNext = daysInCycle - (daysSinceStart % daysInCycle);
            return daysUntilNext;
        }
    }

    public bool ShowRefillWarning => IsActive && DaysUntilRefill <= 7;
}
```

## Create Value Converters (Add to a new file: Converters/ValueConverters.cs)

Create directory first:
```bash
mkdir -p Converters
```

Then create `Converters/ValueConverters.cs`:

```csharp
using System.Globalization;

namespace ScriptzApp.Converters;

public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrWhiteSpace(value?.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colors)
        {
            var colorPair = colors.Split('|');
            if (colorPair.Length == 2)
            {
                return boolValue ? Color.FromArgb(colorPair[0]) : Color.FromArgb(colorPair[1]);
            }
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string texts)
        {
            var textPair = texts.Split('|');
            if (textPair.Length == 2)
            {
                return boolValue ? textPair[0] : textPair[1];
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

## Register Converters in App.xaml

Add this inside the `<ResourceDictionary>` in App.xaml:

```xml
<!-- Converters -->
<converters:IsNotNullOrEmptyConverter x:Key="IsNotNullOrEmptyConverter"/>
<converters:BoolToColorConverter x:Key="BoolToColorConverter"/>
<converters:BoolToTextConverter x:Key="BoolToTextConverter"/>
```

And add this namespace at the top of App.xaml:

```xml
xmlns:converters="clr-namespace:ScriptzApp.Converters"
```

**STOP HERE - Confirm medications list page is created before proceeding to Step 11**
