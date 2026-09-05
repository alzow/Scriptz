using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Features.BusinessSettings.Constants;

namespace QueueApp.Features.BusinessSettings.Models;

// One duration chip. Minutes is null on the Custom chip, which hands the number back to a field
// rather than carrying one — a 20-minute service should not need five chips to exist first.
public sealed class DurationChoice : ObservableObject
{
    public int? Minutes { get; init; }
    public required string Label { get; init; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsCustom => Minutes is null;

    public static DurationChoice Preset(int minutes) =>
        new() { Minutes = minutes, Label = minutes.ToString() };

    public static DurationChoice Custom() =>
        new() { Label = BusinessSettingsConstants.CustomDurationText };
}
