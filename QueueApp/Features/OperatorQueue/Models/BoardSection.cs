using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.OperatorQueue.Models;

// One barber's slice of the board. Sections render in operators.sort_order and never reorder —
// only their heights move. A barber should never have to look for himself, so busyness, urgency
// and who's serving are all deliberately not inputs to the ordering.
//
// Auto-collapse is what makes the density work: an on-shift barber with nobody waiting and nobody
// in the chair is a 62px row, which is how five barbers with two serving fit one screen.
public sealed class BoardSection : ObservableObject
{
    public Guid OperatorId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public int SortOrder { get; init; }

    public bool IsOnShift { get; init; }
    public bool IsOffShift => !IsOnShift;

    public ServingCardItem? Serving { get; init; }
    public bool HasServing => Serving is not null;
    public ObservableCollection<QueueRowItem> Waiting { get; } = new();

    public bool IsExpanded { get; init; }
    public bool IsCollapsed => IsOnShift && !IsExpanded;

    // "1 waiting" / "Free · nobody waiting" / "Serving · 0 waiting" / "Off shift"
    public string StatusText { get; init; } = string.Empty;

    // Ink when somebody is waiting, muted when nobody is. Never purple — purple on this screen
    // belongs to the unassigned pool alone.
    public Color StatusColor { get; init; } = Colors.Transparent;

    public double SectionOpacity => IsOnShift ? 1 : 0.52;

    public bool IsTogglingShift { get; set; }
    public bool IsShiftToggleEnabled => !IsTogglingShift;
}
