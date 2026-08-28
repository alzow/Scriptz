using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed class BayFilterOption : ObservableObject
{
    public Guid? OperatorId { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool IsSelected { get; set; }
}
