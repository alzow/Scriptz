using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using ScriptzApp.Services.Api;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Dashboard;

public partial class DashboardPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IScriptzPopupService _popupService;
    private readonly IApiService _apiService;

    // Benefit tracker (placeholder values until API wired up)
    private const int BenefitTotal = 3500;
    private const int BenefitUsed = 1840;

    [ObservableProperty] private string _welcomeMessage = "Ahmed 👋";
    [ObservableProperty] private string _benefitRemainingText = $"R{BenefitTotal - BenefitUsed} left";
    [ObservableProperty] private double _benefitUsedFraction = (double)BenefitUsed / BenefitTotal;
    [ObservableProperty] private string _benefitSummaryText = $"R{BenefitUsed} used of R{BenefitTotal}";

    [ObservableProperty] private bool _hasUrgentMedication;
    [ObservableProperty] private string _urgentMedicationName = string.Empty;
    [ObservableProperty] private string _urgentMedicationSubText = string.Empty;
    [ObservableProperty] private bool _hasNoMedications;

    public ObservableCollection<ActiveScriptItem> ActiveMedications { get; } = new();

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
        Title = "Home";
    }

    public override async Task OnLoadedAsync(INavigationParameters parameters)
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
                var medications = await _apiService.Api.GetMedicationsAsync();

                ActiveMedications.Clear();

                var active = medications?.Where(m => m.IsActive).ToList() ?? new();

                foreach (var med in active)
                {
                    var daysLeft = 30 - (DateTime.Now - med.StartDate).Days % 30;
                    ActiveMedications.Add(new ActiveScriptItem
                    {
                        Name = med.Name,
                        SubText = $"{med.Dosage} · Laxmi Pharmacy",
                        Status = daysLeft <= 3 ? "Ready" : "Processing",
                        IsReady = daysLeft <= 3,
                        StatusColor = daysLeft <= 3 ? "#2D6A4F" : "#E8621A",
                        StatusBgColor = daysLeft <= 3 ? "#1A2D6A4F" : "#1AE8621A",
                    });
                }

                HasNoMedications = !ActiveMedications.Any();

                // Check for urgent (< 5 days)
                var urgent = active.FirstOrDefault(m =>
                {
                    var d = 30 - (DateTime.Now - m.StartDate).Days % 30;
                    return d <= 5;
                });

                if (urgent != null)
                {
                    var d = 30 - (DateTime.Now - urgent.StartDate).Days % 30;
                    HasUrgentMedication = true;
                    UrgentMedicationName = $"{urgent.Name} running low";
                    UrgentMedicationSubText = $"{d} days remaining · Auto-refill active";
                }
                else
                {
                    HasUrgentMedication = false;
                }
            }
            catch (Exception ex)
            {
                // Offline/demo fallback — show placeholder cards
                ActiveMedications.Add(new ActiveScriptItem
                {
                    Name = "Metformin 500mg",
                    SubText = "60 tabs · Laxmi Pharmacy",
                    Status = "Ready",
                    IsReady = true,
                    StatusColor = "#2D6A4F",
                    StatusBgColor = "#1A2D6A4F",
                });
                ActiveMedications.Add(new ActiveScriptItem
                {
                    Name = "Lisinopril 10mg",
                    SubText = "30 tabs · Laxmi Pharmacy",
                    Status = "Processing",
                    IsReady = false,
                    StatusColor = "#E8621A",
                    StatusBgColor = "#1AE8621A",
                });
                HasNoMedications = false;
                System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
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
        await NavigationService.NavigateAsync("MedicationsListPage");
    }

    [RelayCommand]
    private async Task NavigateToRemindersAsync()
    {
        await NavigationService.NavigateAsync("MedicationsListPage");
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync()
    {
        await NavigationService.NavigateAsync("MedicationsListPage");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await _popupService.ShowConfirmAsync(
            "Sign Out",
            "Are you sure you want to sign out?");

        if (confirm)
        {
            await _authService.LogoutAsync();
            await NavigationService.NavigateAsync("/NavigationPage/LoginPage");
        }
    }
}

public class ActiveScriptItem
{
    public string Name { get; set; } = string.Empty;
    public string SubText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public string StatusColor { get; set; } = "#E8621A";
    public string StatusBgColor { get; set; } = "#1AE8621A";
}
