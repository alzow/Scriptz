using CommunityToolkit.Mvvm.Input;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Medications;

public partial class MedicationDetailPageViewModel : BaseViewModel
{
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
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
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

        // Placeholder until the Queue/pharmacy backend is wired up.
        await ExecuteAsync(async () =>
        {
            await _popupService.ShowAlertAsync("Success",
                IsEditMode ? "Medication updated successfully" : "Medication added successfully");
            await NavigationService.GoBackAsync();
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

        // Placeholder until the Queue/pharmacy backend is wired up.
        await ExecuteAsync(async () =>
        {
            await _popupService.ShowAlertAsync("Success", "Medication deleted successfully");
            await NavigationService.GoBackAsync();
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
