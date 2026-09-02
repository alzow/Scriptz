using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Shared.Domain.Models;

// A row in the operator step. OperatorId is null for the pinned option at the top, and what that
// null means differs by mode: in queue mode it is "Fastest available", which join_queue resolves
// to a real operator inside the insert, so the entry never sits unassigned unless the shop has
// nobody on shift. In booking mode it is "Any available", which stays null on the booking until
// the shop picks who is taking it.
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
