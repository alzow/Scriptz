using CommunityToolkit.Mvvm.Input;
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

    public ObservableCollection<string> FormOptions { get; } = new()
    {
        "Tablet", "Capsule", "Liquid", "Cream", "Injection", "Inhaler", "Drops", "Patch"
    };

    public ObservableCollection<string> FrequencyOptions { get; } = new()
    {
        "Once daily", "Twice daily", "Three times daily", "Four times daily",
        "Every 12 hours", "Every 8 hours", "Every 6 hours",
        "As needed", "Weekly", "Monthly"
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

    public override void Initialize(INavigationParameters parameters)
    {
        base.Initialize(parameters);

        if (parameters.TryGetValue("MedicationId", out object? medicationObj) && medicationObj is string medicationId && !string.IsNullOrEmpty(medicationId))
        {
            _medicationId = medicationId;
            IsEditMode = true;
            PageTitle = "Edit Medication";
            SaveButtonText = "Update Medication";
        }
    }

    public override async Task OnLoadedAsync(INavigationParameters parameters)
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
