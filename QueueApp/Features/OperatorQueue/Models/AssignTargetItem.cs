namespace QueueApp.Features.OperatorQueue.Models;

// A destination row in the assign / move sheet. Sorted soonest-first, off-shift disabled.
//
// The sheet asks who's taking the customer rather than offering "take it myself", because on a
// shared counter phone the app cannot know whose hands are on it.
public sealed class AssignTargetItem
{
    public Guid? OperatorId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public string SubLabel { get; init; } = string.Empty;
    public bool ShowSoonestTag { get; set; }
    public bool IsSelectable { get; init; } = true;
    public bool ShowPresenceDot { get; init; }

    // The pool row — assigning back to null returns the entry to the shared pool, which is a real
    // destination and has to be offered, not just arrived at by accident.
    public bool IsPool { get; init; }

    public double RowOpacity => IsSelectable ? 1 : 0.4;
    public double SortWaitMinutes { get; init; }
}
