using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Shared.Domain.Models;

public sealed class DayChoiceItem : ObservableObject
{
    public DateTime Date { get; init; }
    public string DayOfWeekText { get; init; } = string.Empty;
    public string DayNumberText { get; init; } = string.Empty;

    // Slot counts are per selected operator until the multi-resource union lands, so FreeText names
    // the operator ("7 free · Bay 2") rather than implying the whole shop has seven slots open.
    public string FreeText { get; set; } = "…";
    public bool IsFull { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsSelectable => !IsFull;
}
