namespace QueueApp.Features.Flow.Visit.Models;

public sealed class VisitFactRow
{
    public required string Label { get; init; }
    public required string Value { get; init; }
    public bool IsMono { get; init; } = true;
}
