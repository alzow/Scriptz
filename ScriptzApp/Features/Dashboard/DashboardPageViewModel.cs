using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptzApp.Constants;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Dashboard;

public partial class DashboardPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IScriptzPopupService _popupService;

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
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
    }

    public override Task OnLoadedAsync(INavigationParameters parameters)
    {
        // LoadDashboardData();
        return Task.CompletedTask;
    }

    // Placeholder cards until the Queue/pharmacy backend is wired up.
    private void LoadDashboardData()
    {
        ActiveMedications.Clear();
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
        HasUrgentMedication = false;
    }

    [RelayCommand]
    private async Task NavigateToMedicationsAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.MedicationsListPage);
    }

    [RelayCommand]
    private async Task NavigateToPrescriptionsAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.MedicationsListPage);
    }

    [RelayCommand]
    private async Task NavigateToRemindersAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.MedicationsListPage);
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.MedicationsListPage);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await _popupService.ShowConfirmAsync(
            "Sign Out",
            "Are you sure you want to sign out?");

        if (confirm)
        {
            await _authService.ClearSessionAsync();
            await NavigationService.NavigateAsync(NavigationPaths.Login);
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
