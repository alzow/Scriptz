using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Models.Api.Responses;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;
using ScriptzApp.Services.Api;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Medications;

public partial class MedicationsListPageViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IScriptzPopupService _popupService;

    // ── Tab state ──────────────────────────────────────────────────────────────
    private string _selectedTab = "active";

    [ObservableProperty] private bool _isActiveTab = true;
    [ObservableProperty] private bool _isSubmitTab;
    [ObservableProperty] private bool _isHistoryTab;

    // Active tab styling
    [ObservableProperty] private string _activeTabBg = "#FFFFFF";
    [ObservableProperty] private string _activeTabTextColor = "#2A1A0E";
    [ObservableProperty] private FontAttributes _activeTabFontAttr = FontAttributes.Bold;
    // Submit tab styling
    [ObservableProperty] private string _submitTabBg = "Transparent";
    [ObservableProperty] private string _submitTabTextColor = "#A0826A";
    [ObservableProperty] private FontAttributes _submitTabFontAttr = FontAttributes.None;
    // History tab styling
    [ObservableProperty] private string _historyTabBg = "Transparent";
    [ObservableProperty] private string _historyTabTextColor = "#A0826A";
    [ObservableProperty] private FontAttributes _historyTabFontAttr = FontAttributes.None;

    // ── Script data ────────────────────────────────────────────────────────────
    public ObservableCollection<ScriptDisplayModel> ActiveScripts { get; } = new();
    public ObservableCollection<ScriptDisplayModel> DeliveredScripts { get; } = new();

    [ObservableProperty] private bool _hasNoActiveScripts;
    [ObservableProperty] private bool _hasNoHistory;
    [ObservableProperty] private bool _isRefreshing;

    // ── Submit form fields ─────────────────────────────────────────────────────
    [ObservableProperty] private string _patientName = string.Empty;
    [ObservableProperty] private string _doctorName = string.Empty;
    [ObservableProperty] private string _medicationNotes = string.Empty;
    [ObservableProperty] private string _pharmacyNotes = string.Empty;
    [ObservableProperty] private string _deliveryAddress = string.Empty;

    // Legacy bindings kept so existing navigation still compiles
    public ObservableCollection<MedicationDisplayModel> Medications { get; } = new();
    public int ActiveCount { get; set; }
    public int ChronicCount { get; set; }
    public int RefillsDueCount { get; set; }

    public MedicationsListPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IApiService apiService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _apiService = apiService;
        _popupService = popupService;
        Title = "My Scripts";
    }

    public override async Task OnLoadedAsync(INavigationParameters parameters)
    {
        await base.OnLoadedAsync(parameters);
        await LoadMedicationsAsync();
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        _selectedTab = tab;
        IsActiveTab = tab == "active";
        IsSubmitTab = tab == "submit";
        IsHistoryTab = tab == "history";
        RefreshTabStyles();
    }

    private void RefreshTabStyles()
    {
        ActiveTabBg = _selectedTab == "active" ? "#FFFFFF" : "Transparent";
        ActiveTabTextColor = _selectedTab == "active" ? "#2A1A0E" : "#A0826A";
        ActiveTabFontAttr = _selectedTab == "active" ? FontAttributes.Bold : FontAttributes.None;

        SubmitTabBg = _selectedTab == "submit" ? "#FFFFFF" : "Transparent";
        SubmitTabTextColor = _selectedTab == "submit" ? "#2A1A0E" : "#A0826A";
        SubmitTabFontAttr = _selectedTab == "submit" ? FontAttributes.Bold : FontAttributes.None;

        HistoryTabBg = _selectedTab == "history" ? "#FFFFFF" : "Transparent";
        HistoryTabTextColor = _selectedTab == "history" ? "#2A1A0E" : "#A0826A";
        HistoryTabFontAttr = _selectedTab == "history" ? FontAttributes.Bold : FontAttributes.None;
    }

    [RelayCommand]
    private async Task LoadMedicationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                var medications = await _apiService.Api.GetMedicationsAsync();

                ActiveScripts.Clear();
                DeliveredScripts.Clear();
                Medications.Clear();

                if (medications != null)
                {
                    foreach (var med in medications.OrderBy(m => m.Name))
                    {
                        var daysLeft = 30 - (DateTime.Now - med.StartDate).Days % 30;
                        var status = daysLeft <= 3 ? "Ready" : "Processing";

                        var script = new ScriptDisplayModel
                        {
                            Id = med.Id,
                            Name = med.Name,
                            QuantityAndRef = $"{med.Dosage} · RX-{med.Id}",
                            RefillsText = "3 refills remaining",
                            Status = med.IsActive ? status : "Delivered",
                            IsReady = med.IsActive && daysLeft <= 3,
                            IsProcessing = med.IsActive && daysLeft > 3,
                        };
                        script.ApplyStatusColors();

                        if (med.IsActive)
                            ActiveScripts.Add(script);
                        else
                            DeliveredScripts.Add(script);

                        Medications.Add(new MedicationDisplayModel(med));
                    }

                    ActiveCount = medications.Count(m => m.IsActive);
                    ChronicCount = medications.Count(m => m.IsActive);
                    RefillsDueCount = ActiveScripts.Count(s => s.IsReady);
                }

                HasNoActiveScripts = !ActiveScripts.Any();
                HasNoHistory = !DeliveredScripts.Any();
            }
            catch (Exception ex)
            {
                // Demo fallback
                ActiveScripts.Add(ScriptDisplayModel.Demo("Metformin 500mg", "60 tabs · RX-2401", "Ready"));
                ActiveScripts.Add(ScriptDisplayModel.Demo("Lisinopril 10mg", "30 tabs · RX-2398", "Processing"));
                DeliveredScripts.Add(ScriptDisplayModel.Demo("Atorvastatin 20mg", "30 tabs · RX-2390", "Delivered"));
                HasNoActiveScripts = false;
                HasNoHistory = false;
                System.Diagnostics.Debug.WriteLine($"Scripts load error: {ex.Message}");
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
        // From Submit tab — navigate to detail / show success
        await NavigationService.NavigateAsync("MedicationDetailPage");
    }

    [RelayCommand]
    private async Task ViewMedicationAsync(MedicationDisplayModel medication)
    {
        await NavigationService.NavigateAsync($"MedicationDetailPage?MedicationId={medication.Id}");
    }
}

// ── Display models ─────────────────────────────────────────────────────────────

public class ScriptDisplayModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string QuantityAndRef { get; set; } = string.Empty;
    public string RefillsText { get; set; } = "3 refills remaining";
    public string Status { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool IsProcessing { get; set; }

    public string StatusColor { get; set; } = "#E8621A";
    public string StatusBgColor { get; set; } = "#1AE8621A";

    // Step indicator colors
    private static readonly string DoneColor = "#2D6A4F";
    private static readonly string ActiveColor = "#E8621A";
    private static readonly string InactiveColor = "#E8DDD0";

    public string Step1Color { get; set; } = InactiveColor;
    public string Step2Color { get; set; } = InactiveColor;
    public string Step3Color { get; set; } = InactiveColor;
    public string Step4Color { get; set; } = InactiveColor;
    public string Line1Color { get; set; } = InactiveColor;
    public string Line2Color { get; set; } = InactiveColor;
    public string Line3Color { get; set; } = InactiveColor;
    public FontAttributes Step1FontAttr { get; set; } = FontAttributes.None;
    public FontAttributes Step2FontAttr { get; set; } = FontAttributes.None;
    public FontAttributes Step3FontAttr { get; set; } = FontAttributes.None;
    public FontAttributes Step4FontAttr { get; set; } = FontAttributes.None;

    public void ApplyStatusColors()
    {
        switch (Status)
        {
            case "Ready":
                StatusColor = "#2D6A4F";
                StatusBgColor = "#1A2D6A4F";
                Step1Color = DoneColor; Step1FontAttr = FontAttributes.Bold;
                Step2Color = DoneColor; Step2FontAttr = FontAttributes.Bold;
                Step3Color = DoneColor; Step3FontAttr = FontAttributes.Bold;
                Step4Color = InactiveColor;
                Line1Color = DoneColor; Line2Color = DoneColor; Line3Color = InactiveColor;
                break;
            case "Processing":
                StatusColor = "#E8621A";
                StatusBgColor = "#1AE8621A";
                Step1Color = DoneColor; Step1FontAttr = FontAttributes.Bold;
                Step2Color = ActiveColor; Step2FontAttr = FontAttributes.Bold;
                Step3Color = InactiveColor;
                Step4Color = InactiveColor;
                Line1Color = DoneColor; Line2Color = InactiveColor; Line3Color = InactiveColor;
                break;
            case "Delivered":
                StatusColor = "#B07850";
                StatusBgColor = "#1AB07850";
                Step1Color = DoneColor; Step1FontAttr = FontAttributes.Bold;
                Step2Color = DoneColor; Step2FontAttr = FontAttributes.Bold;
                Step3Color = DoneColor; Step3FontAttr = FontAttributes.Bold;
                Step4Color = DoneColor; Step4FontAttr = FontAttributes.Bold;
                Line1Color = DoneColor; Line2Color = DoneColor; Line3Color = DoneColor;
                break;
            default:
                StatusColor = "#E8621A";
                StatusBgColor = "#1AE8621A";
                Step1Color = ActiveColor; Step1FontAttr = FontAttributes.Bold;
                Line1Color = InactiveColor; Line2Color = InactiveColor; Line3Color = InactiveColor;
                break;
        }
    }

    public static ScriptDisplayModel Demo(string name, string quantityRef, string status)
    {
        var m = new ScriptDisplayModel
        {
            Name = name,
            QuantityAndRef = quantityRef,
            RefillsText = "3 refills remaining",
            Status = status,
            IsReady = status == "Ready",
            IsProcessing = status == "Processing",
        };
        m.ApplyStatusColors();
        return m;
    }
}

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

    public string SubText => $"{Dosage} · Laxmi Pharmacy";
    public string Status => IsActive ? "Ready" : "Delivered";
    public bool IsReady => IsActive;
    public string StatusColor => IsActive ? "#2D6A4F" : "#B07850";
    public string StatusBgColor => IsActive ? "#1A2D6A4F" : "#1AB07850";

    public int DaysUntilRefill
    {
        get
        {
            var daysSinceStart = (DateTime.Now - StartDate).Days;
            var daysUntilNext = 30 - daysSinceStart % 30;
            return daysUntilNext;
        }
    }

    public bool ShowRefillWarning => IsActive && DaysUntilRefill <= 7;
}
