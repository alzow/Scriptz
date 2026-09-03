using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Shared.Domain.Models;

public sealed class SlotChoiceItem : ObservableObject
{
    public required SlotResponse Slot { get; init; }
    public string TimeText { get; init; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
