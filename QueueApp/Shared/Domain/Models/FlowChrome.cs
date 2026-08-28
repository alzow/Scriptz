using QueueApp.Shared.Domain;

namespace QueueApp.Shared.Domain.Models;

// One segment of the progress rail. Rebuilt on every step change — there are at most four — so the
// triggers that colour it only ever evaluate against a fresh, immutable row.
public sealed class RailSegment
{
    public bool IsDone { get; init; }
    public bool IsCurrent { get; init; }
}

// A completed step, shown under the rail and tappable to jump back to it.
public sealed class CrumbChip
{
    public FlowStep Step { get; init; }
    public string Text { get; init; } = string.Empty;
}
