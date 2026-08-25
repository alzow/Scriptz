using QueueApp.Features.BusinessDetail.Flow;

namespace QueueApp.Features.BusinessDetail.Models;

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

// A dot in the confirmation card's queue strip, built by ITicketScheme.
public sealed class TicketDot
{
    public string Label { get; init; } = string.Empty;
    public bool IsNowServing { get; init; }
    public bool IsMine { get; init; }
}
