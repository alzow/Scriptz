using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.BookingAgenda;

// Per-resource separation is a filter, not a layout — one chronological list stays the layout even
// when the operator genuinely wants to look at one bay (spec §2).
public sealed class BayFilterOption : ObservableObject
{
    public Guid? OperatorId { get; init; }
    public string Label { get; init; } = "";
    public bool IsSelected { get; set; }
}
