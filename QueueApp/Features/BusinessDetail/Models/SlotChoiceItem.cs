using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Features.BusinessDetail.Models;

public sealed class SlotChoiceItem : ObservableObject
{
    public required SlotResponse Slot { get; init; }
    public string TimeText { get; init; } = string.Empty;
    public bool IsSelected { get; set; }
}
