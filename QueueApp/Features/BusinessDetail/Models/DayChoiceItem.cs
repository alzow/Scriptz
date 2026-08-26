using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.BusinessDetail.Models;

public sealed class DayChoiceItem : ObservableObject
{
    public DateTime Date { get; init; }
    public string DayOfWeekText { get; init; } = string.Empty;
    public string DayNumberText { get; init; } = string.Empty;

    // Slot counts are per selected operator until the multi-resource union lands, so FreeText names
    // the operator ("7 free · Bay 2") rather than implying the whole shop has seven slots open.
    public string FreeText { get; set; } = "…";
    public bool IsFull { get; set; }
    public bool IsSelected { get; set; }

    public double DayOpacity => IsFull ? 0.35 : 1;
    public bool IsSelectable => !IsFull;
}
