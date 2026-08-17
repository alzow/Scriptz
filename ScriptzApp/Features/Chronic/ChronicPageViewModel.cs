using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Storage;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Chronic;

public partial class ChronicPageViewModel : BaseViewModel
{
    public ObservableCollection<ChronicMedItem> ChronicMeds { get; } = new()
    {
        new ChronicMedItem("Metformin 500mg",   "Daily · 2x",  4,  true,  "Tomorrow"),
        new ChronicMedItem("Lisinopril 10mg",   "Daily · 1x",  11, true,  "In 6 days"),
        new ChronicMedItem("Vitamin D3 1000IU", "Daily · 1x",  21, false, null),
    };

    public List<HowItWorksStep> HowItWorksSteps { get; } = new()
    {
        new("1", "Your script is loaded once by Laxmi Pharmacy"),
        new("2", "App tracks your daily supply"),
        new("3", "Order is placed automatically 5 days before you run out"),
        new("4", "Delivered to your door · Pay on delivery"),
    };

    public ChronicPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
        Title = "Chronic";
    }

    [RelayCommand]
    private void ToggleAutoRefill(ChronicMedItem med)
    {
        med.AutoRefill = !med.AutoRefill;
    }
}

public partial class ChronicMedItem : ObservableObject
{
    public string Name { get; }
    public string Schedule { get; }
    public int DaysLeft { get; }
    public string? NextOrder { get; }

    [ObservableProperty] private bool _autoRefill;

    public string DaysLeftText => $"{DaysLeft} days";
    public double SupplyFraction => Math.Min(1.0, DaysLeft / 30.0);
    public bool IsUrgent => DaysLeft <= 5;
    public string BarColor => IsUrgent ? "#E8621A" : "#1A7A6E";
    public string StockLabel => IsUrgent ? "Low Stock" : "In Stock";
    public string StockColor => IsUrgent ? "#E8621A" : "#2D6A4F";
    public string StockBg => IsUrgent ? "#FDF0E8" : "#EAF4EE";
    public string NextOrderText => NextOrder != null ? $"Next order: {NextOrder}" : string.Empty;

    public ChronicMedItem(string name, string schedule, int daysLeft, bool autoRefill, string? nextOrder)
    {
        Name = name;
        Schedule = schedule;
        DaysLeft = daysLeft;
        _autoRefill = autoRefill;
        NextOrder = nextOrder;
    }
}

public record HowItWorksStep(string Number, string Text);
