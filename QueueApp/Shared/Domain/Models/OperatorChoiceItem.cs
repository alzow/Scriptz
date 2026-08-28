using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Shared.Domain.Models;

// A row in the operator step. OperatorId is null for the pinned "Any available" option, which is a
// real choice in queue mode — queue_entries.operator_id is nullable — and never offered in booking
// mode, where bookings.operator_id is NOT NULL.
public sealed class OperatorChoiceItem : ObservableObject
{
    public Guid? OperatorId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public string SubLabel { get; init; } = string.Empty;
    public bool IsAnyAvailable { get; init; }
    public bool ShowFastestTag { get; init; }
    public bool IsSelected { get; set; }
}
