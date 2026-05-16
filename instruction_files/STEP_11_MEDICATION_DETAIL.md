# STEP 11: Create Medication Detail/Edit Page (Scriptz UI Design)

This step creates the medication detail/add/edit page with chronic medication auto-refill features from the Scriptz prototype.

## Create Features/Medications/MedicationDetailPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:ScriptzApp.Features.Medications"
             x:Class="ScriptzApp.Features.Medications.MedicationDetailPage"
             x:DataType="vm:MedicationDetailPageViewModel"
             Title="{Binding PageTitle}">
    
    <Grid RowDefinitions="*,Auto" BackgroundColor="{StaticResource Gray50}">
        
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Padding="20" Spacing="16">

                <!-- Header Icon -->
                <Frame WidthRequest="80"
                       HeightRequest="80"
                       CornerRadius="40"
                       Padding="0"
                       HasShadow="True"
                       HorizontalOptions="Center"
                       Margin="0,10,0,20"
                       BackgroundColor="{StaticResource White}">
                    <Label Text="💊"
                           FontSize="48"
                           HorizontalOptions="Center"
                           VerticalOptions="Center"/>
                </Frame>

                <!-- Medication Name -->
                <VerticalStackLayout Spacing="8">
                    <Label Text="Medication Name *"
                           Style="{StaticResource Body}"
                           FontAttributes="Bold"
                           TextColor="{StaticResource Gray700}"/>
                    <Border BackgroundColor="{StaticResource White}"
                            Stroke="{StaticResource Gray300}"
                            StrokeThickness="1"
                            Padding="15,12">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="10"/>
                        </Border.StrokeShape>
                        <Entry Placeholder="e.g., Metformin"
                               Text="{Binding Name}"
                               TextColor="{StaticResource Gray900}"
                               PlaceholderColor="{StaticResource Gray400}"/>
                    </Border>
                </VerticalStackLayout>

                <!-- Generic Name -->
                <VerticalStackLayout Spacing="8">
                    <Label Text="Generic Name"
                           Style="{StaticResource Body}"
                           FontAttributes="Bold"
                           TextColor="{StaticResource Gray700}"/>
                    <Border BackgroundColor="{StaticResource White}"
                            Stroke="{StaticResource Gray300}"
                            StrokeThickness="1"
                            Padding="15,12">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="10"/>
                        </Border.StrokeShape>
                        <Entry Placeholder="e.g., Metformin Hydrochloride"
                               Text="{Binding GenericName}"
                               TextColor="{StaticResource Gray900}"
                               PlaceholderColor="{StaticResource Gray400}"/>
                    </Border>
                </VerticalStackLayout>

                <!-- Dosage & Form Row -->
                <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                    <!-- Dosage -->
                    <VerticalStackLayout Grid.Column="0" Spacing="8">
                        <Label Text="Dosage *"
                               Style="{StaticResource Body}"
                               FontAttributes="Bold"
                               TextColor="{StaticResource Gray700}"/>
                        <Border BackgroundColor="{StaticResource White}"
                                Stroke="{StaticResource Gray300}"
                                StrokeThickness="1"
                                Padding="15,12">
                            <Border.StrokeShape>
                                <RoundRectangle CornerRadius="10"/>
                            </Border.StrokeShape>
                            <Entry Placeholder="500mg"
                                   Text="{Binding Dosage}"
                                   TextColor="{StaticResource Gray900}"
                                   PlaceholderColor="{StaticResource Gray400}"/>
                        </Border>
                    </VerticalStackLayout>

                    <!-- Form -->
                    <VerticalStackLayout Grid.Column="1" Spacing="8">
                        <Label Text="Form *"
                               Style="{StaticResource Body}"
                               FontAttributes="Bold"
                               TextColor="{StaticResource Gray700}"/>
                        <Border BackgroundColor="{StaticResource White}"
                                Stroke="{StaticResource Gray300}"
                                StrokeThickness="1"
                                Padding="15,12">
                            <Border.StrokeShape>
                                <RoundRectangle CornerRadius="10"/>
                            </Border.StrokeShape>
                            <Picker ItemsSource="{Binding FormOptions}"
                                    SelectedItem="{Binding Form}"
                                    TextColor="{StaticResource Gray900}"
                                    Title="Select form"/>
                        </Border>
                    </VerticalStackLayout>
                </Grid>

                <!-- Frequency -->
                <VerticalStackLayout Spacing="8">
                    <Label Text="Frequency *"
                           Style="{StaticResource Body}"
                           FontAttributes="Bold"
                           TextColor="{StaticResource Gray700}"/>
                    <Border BackgroundColor="{StaticResource White}"
                            Stroke="{StaticResource Gray300}"
                            StrokeThickness="1"
                            Padding="15,12">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="10"/>
                        </Border.StrokeShape>
                        <Picker ItemsSource="{Binding FrequencyOptions}"
                                SelectedItem="{Binding Frequency}"
                                TextColor="{StaticResource Gray900}"
                                Title="Select frequency"/>
                    </Border>
                </VerticalStackLayout>

                <!-- Instructions -->
                <VerticalStackLayout Spacing="8">
                    <Label Text="Instructions"
                           Style="{StaticResource Body}"
                           FontAttributes="Bold"
                           TextColor="{StaticResource Gray700}"/>
                    <Border BackgroundColor="{StaticResource White}"
                            Stroke="{StaticResource Gray300}"
                            StrokeThickness="1"
                            Padding="15,12">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="10"/>
                        </Border.StrokeShape>
                        <Editor Placeholder="e.g., Take with food, avoid alcohol"
                                Text="{Binding Instructions}"
                                TextColor="{StaticResource Gray900}"
                                PlaceholderColor="{StaticResource Gray400}"
                                AutoSize="TextChanges"
                                MinimumHeightRequest="80"/>
                    </Border>
                </VerticalStackLayout>

                <!-- Start & End Date Row -->
                <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                    <!-- Start Date -->
                    <VerticalStackLayout Grid.Column="0" Spacing="8">
                        <Label Text="Start Date *"
                               Style="{StaticResource Body}"
                               FontAttributes="Bold"
                               TextColor="{StaticResource Gray700}"/>
                        <Border BackgroundColor="{StaticResource White}"
                                Stroke="{StaticResource Gray300}"
                                StrokeThickness="1"
                                Padding="15,12">
                            <Border.StrokeShape>
                                <RoundRectangle CornerRadius="10"/>
                            </Border.StrokeShape>
                            <DatePicker Date="{Binding StartDate}"
                                        TextColor="{StaticResource Gray900}"/>
                        </Border>
                    </VerticalStackLayout>

                    <!-- End Date -->
                    <VerticalStackLayout Grid.Column="1" Spacing="8">
                        <Label Text="End Date"
                               Style="{StaticResource Body}"
                               FontAttributes="Bold"
                               TextColor="{StaticResource Gray700}"/>
                        <Border BackgroundColor="{StaticResource White}"
                                Stroke="{StaticResource Gray300}"
                                StrokeThickness="1"
                                Padding="15,12">
                            <Border.StrokeShape>
                                <RoundRectangle CornerRadius="10"/>
                            </Border.StrokeShape>
                            <DatePicker Date="{Binding EndDate}"
                                        TextColor="{StaticResource Gray900}"
                                        IsVisible="{Binding HasEndDate}"/>
                        </Border>
                    </VerticalStackLayout>
                </Grid>

                <!-- Chronic Medication Card -->
                <Frame BackgroundColor="{StaticResource White}"
                       BorderColor="{StaticResource Primary}"
                       CornerRadius="12"
                       Padding="16"
                       Margin="0,8,0,0"
                       HasShadow="True">
                    <VerticalStackLayout Spacing="12">
                        <HorizontalStackLayout Spacing="12">
                            <Label Text="🔄"
                                   FontSize="28"
                                   VerticalOptions="Center"/>
                            <VerticalStackLayout Spacing="4" HorizontalOptions="FillAndExpand">
                                <Label Text="Chronic Medication Auto-Refill"
                                       Style="{StaticResource Body}"
                                       FontAttributes="Bold"
                                       TextColor="{StaticResource Gray900}"/>
                                <Label Text="Automatically reorder when running low"
                                       Style="{StaticResource Caption}"
                                       TextColor="{StaticResource Gray600}"/>
                            </VerticalStackLayout>
                            <Switch IsToggled="{Binding IsChronic}"
                                    OnColor="{StaticResource Primary}"
                                    VerticalOptions="Center"/>
                        </HorizontalStackLayout>

                        <!-- Refill Settings (only visible if chronic) -->
                        <VerticalStackLayout Spacing="8" IsVisible="{Binding IsChronic}">
                            <BoxView HeightRequest="1" BackgroundColor="{StaticResource Gray200}"/>
                            
                            <Label Text="Refill when supply reaches:"
                                   Style="{StaticResource Caption}"
                                   TextColor="{StaticResource Gray700}"/>
                            
                            <Grid ColumnDefinitions="*,*,*" ColumnSpacing="8">
                                <Button Grid.Column="0"
                                        Text="3 days"
                                        Command="{Binding SetRefillDaysCommand}"
                                        CommandParameter="3"
                                        BackgroundColor="{Binding RefillDays, Converter={StaticResource EqualsToColorConverter}, ConverterParameter='3|{StaticResource Primary}|{StaticResource Gray200}'}"
                                        TextColor="{Binding RefillDays, Converter={StaticResource EqualsToColorConverter}, ConverterParameter='3|{StaticResource White}|{StaticResource Gray600}'}"
                                        CornerRadius="8"
                                        FontSize="14"
                                        Padding="0"
                                        HeightRequest="36"/>
                                
                                <Button Grid.Column="1"
                                        Text="5 days"
                                        Command="{Binding SetRefillDaysCommand}"
                                        CommandParameter="5"
                                        BackgroundColor="{Binding RefillDays, Converter={StaticResource EqualsToColorConverter}, ConverterParameter='5|{StaticResource Primary}|{StaticResource Gray200}'}"
                                        TextColor="{Binding RefillDays, Converter={StaticResource EqualsToColorConverter}, ConverterParameter='5|{StaticResource White}|{StaticResource Gray600}'}"
                                        CornerRadius="8"
                                        FontSize="14"
                                        Padding="0"
                                        HeightRequest="36"/>
                                
                                <Button Grid.Column="2"
                                        Text="7 days"
                                        Command="{Binding SetRefillDaysCommand}"
                                        CommandParameter="7"
                                        BackgroundColor="{Binding RefillDays, Converter={StaticResource EqualsToColorConverter}, ConverterParameter='7|{StaticResource Primary}|{StaticResource Gray200}'}"
                                        TextColor="{Binding RefillDays, Converter={StaticResource EqualsToColorConverter}, ConverterParameter='7|{StaticResource White}|{StaticResource Gray600}'}"
                                        CornerRadius="8"
                                        FontSize="14"
                                        Padding="0"
                                        HeightRequest="36"/>
                            </Grid>
                        </VerticalStackLayout>
                    </VerticalStackLayout>
                </Frame>

                <!-- Active Status Toggle -->
                <Frame BackgroundColor="{StaticResource White}"
                       BorderColor="{StaticResource Gray300}"
                       CornerRadius="12"
                       Padding="16"
                       HasShadow="False">
                    <HorizontalStackLayout Spacing="12">
                        <VerticalStackLayout Spacing="4" HorizontalOptions="FillAndExpand">
                            <Label Text="Active Medication"
                                   Style="{StaticResource Body}"
                                   FontAttributes="Bold"
                                   TextColor="{StaticResource Gray900}"/>
                            <Label Text="Currently taking this medication"
                                   Style="{StaticResource Caption}"
                                   TextColor="{StaticResource Gray600}"/>
                        </VerticalStackLayout>
                        <Switch IsToggled="{Binding IsActive}"
                                OnColor="{StaticResource Success}"
                                VerticalOptions="Center"/>
                    </HorizontalStackLayout>
                </Frame>

                <!-- Delete Button (only show if editing) -->
                <Button Text="🗑️ Delete Medication"
                        Command="{Binding DeleteMedicationCommand}"
                        IsVisible="{Binding IsEditMode}"
                        BackgroundColor="Transparent"
                        BorderColor="{StaticResource Error}"
                        BorderWidth="2"
                        TextColor="{StaticResource Error}"
                        CornerRadius="12"
                        HeightRequest="50"
                        Margin="0,20,0,10"/>

            </VerticalStackLayout>
        </ScrollView>

        <!-- Bottom Action Buttons -->
        <Grid Grid.Row="1" 
              ColumnDefinitions="*,*" 
              ColumnSpacing="12"
              Padding="20"
              BackgroundColor="{StaticResource White}">
            
            <Button Grid.Column="0"
                    Text="Cancel"
                    Command="{Binding CancelCommand}"
                    BackgroundColor="Transparent"
                    BorderColor="{StaticResource Gray400}"
                    BorderWidth="2"
                    TextColor="{StaticResource Gray700}"
                    CornerRadius="12"
                    HeightRequest="50"/>
            
            <Button Grid.Column="1"
                    Text="{Binding SaveButtonText}"
                    Command="{Binding SaveCommand}"
                    Background="{StaticResource PrimaryGradient}"
                    TextColor="{StaticResource White}"
                    FontAttributes="Bold"
                    CornerRadius="12"
                    HeightRequest="50"/>
        </Grid>

        <!-- Loading Overlay -->
        <Grid Grid.Row="0" Grid.RowSpan="2"
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

## Create Features/Medications/MedicationDetailPage.xaml.cs

```csharp
namespace ScriptzApp.Features.Medications;

public partial class MedicationDetailPage : ContentPage
{
    public MedicationDetailPage()
    {
        InitializeComponent();
    }
}
```

## Create Features/Medications/MedicationDetailPageViewModel.cs

```csharp
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using ScriptzApp.Services.Api;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Medications;

public partial class MedicationDetailPageViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IScriptzPopupService _popupService;

    private string? _medicationId;

    public bool IsEditMode { get; set; }
    public string PageTitle { get; set; } = "Add Medication";
    public string SaveButtonText { get; set; } = "Save Medication";

    // Medication Fields
    public string Name { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Form { get; set; } = "Tablet";
    public string Frequency { get; set; } = "Once daily";
    public string Instructions { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
    public bool HasEndDate { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    // Chronic medication settings
    public bool IsChronic { get; set; } = false;
    public int RefillDays { get; set; } = 5;

    // Options
    public ObservableCollection<string> FormOptions { get; } = new()
    {
        "Tablet",
        "Capsule",
        "Liquid",
        "Cream",
        "Injection",
        "Inhaler",
        "Drops",
        "Patch"
    };

    public ObservableCollection<string> FrequencyOptions { get; } = new()
    {
        "Once daily",
        "Twice daily",
        "Three times daily",
        "Four times daily",
        "Every 12 hours",
        "Every 8 hours",
        "Every 6 hours",
        "As needed",
        "Weekly",
        "Monthly"
    };

    public MedicationDetailPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IApiService apiService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _apiService = apiService;
        _popupService = popupService;
    }

    public override void Initialize(NavigationParameters parameters)
    {
        base.Initialize(parameters);

        if (parameters.TryGetValue("MedicationId", out string? medicationId) && !string.IsNullOrEmpty(medicationId))
        {
            _medicationId = medicationId;
            IsEditMode = true;
            PageTitle = "Edit Medication";
            SaveButtonText = "Update Medication";
        }
    }

    public override async Task OnLoadedAsync(NavigationParameters parameters)
    {
        await base.OnLoadedAsync(parameters);

        if (IsEditMode && !string.IsNullOrEmpty(_medicationId))
        {
            await LoadMedicationAsync();
        }
    }

    private async Task LoadMedicationAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                var medication = await _apiService.Api.GetMedicationByIdAsync(_medicationId!);

                if (medication != null)
                {
                    Name = medication.Name;
                    GenericName = medication.GenericName;
                    Dosage = medication.Dosage;
                    Form = medication.Form;
                    Frequency = medication.Frequency;
                    Instructions = medication.Instructions;
                    StartDate = medication.StartDate;
                    EndDate = medication.EndDate ?? DateTime.Today.AddMonths(1);
                    HasEndDate = medication.EndDate.HasValue;
                    IsActive = medication.IsActive;
                    
                    // Determine if chronic based on frequency
                    IsChronic = Frequency.Contains("daily", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                await _popupService.ShowAlertAsync("Error", "Failed to load medication details.");
                System.Diagnostics.Debug.WriteLine($"Error loading medication: {ex.Message}");
            }
        });
    }

    [RelayCommand]
    private void SetRefillDays(string days)
    {
        if (int.TryParse(days, out int daysValue))
        {
            RefillDays = daysValue;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Dosage) || 
            string.IsNullOrWhiteSpace(Form) || string.IsNullOrWhiteSpace(Frequency))
        {
            await _popupService.ShowAlertAsync("Validation Error", "Please fill in all required fields (*)");
            return;
        }

        await ExecuteAsync(async () =>
        {
            try
            {
                if (IsEditMode)
                {
                    // Update existing medication
                    var request = new UpdateMedicationRequest
                    {
                        Name = Name.Trim(),
                        GenericName = GenericName.Trim(),
                        Dosage = Dosage.Trim(),
                        Form = Form,
                        Frequency = Frequency,
                        Instructions = Instructions.Trim(),
                        StartDate = StartDate,
                        EndDate = HasEndDate ? EndDate : null,
                        IsActive = IsActive
                    };

                    await _apiService.Api.UpdateMedicationAsync(_medicationId!, request);
                    await _popupService.ShowAlertAsync("Success", "Medication updated successfully");
                }
                else
                {
                    // Create new medication
                    var request = new CreateMedicationRequest
                    {
                        Name = Name.Trim(),
                        GenericName = GenericName.Trim(),
                        Dosage = Dosage.Trim(),
                        Form = Form,
                        Frequency = Frequency,
                        Instructions = Instructions.Trim(),
                        StartDate = StartDate,
                        EndDate = HasEndDate ? EndDate : null,
                        IsActive = IsActive
                    };

                    await _apiService.Api.CreateMedicationAsync(request);
                    await _popupService.ShowAlertAsync("Success", "Medication added successfully");
                }

                // Navigate back
                await NavigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                await _popupService.ShowAlertAsync("Error", "Failed to save medication. Please try again.");
                System.Diagnostics.Debug.WriteLine($"Error saving medication: {ex.Message}");
            }
        });
    }

    [RelayCommand]
    private async Task DeleteMedicationAsync()
    {
        var confirm = await _popupService.ShowConfirmAsync(
            "Delete Medication",
            "Are you sure you want to delete this medication? This action cannot be undone.");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            try
            {
                await _apiService.Api.DeleteMedicationAsync(_medicationId!);
                await _popupService.ShowAlertAsync("Success", "Medication deleted successfully");
                await NavigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                await _popupService.ShowAlertAsync("Error", "Failed to delete medication.");
                System.Diagnostics.Debug.WriteLine($"Error deleting medication: {ex.Message}");
            }
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
```

## Add EqualsToColorConverter to Converters/ValueConverters.cs

Add this converter to the existing file:

```csharp
public class EqualsToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 3)
            {
                var compareValue = parts[0];
                var trueColor = parts[1];
                var falseColor = parts[2];

                bool isEqual = value?.ToString() == compareValue;
                return isEqual ? Color.FromArgb(trueColor) : Color.FromArgb(falseColor);
            }
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

And register it in App.xaml:

```xml
<converters:EqualsToColorConverter x:Key="EqualsToColorConverter"/>
```

**STOP HERE - Confirm medication detail page is created before proceeding to Step 12**
