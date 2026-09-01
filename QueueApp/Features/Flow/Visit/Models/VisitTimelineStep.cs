namespace QueueApp.Features.Flow.Visit.Models;

public enum VisitStepState
{
    Done,
    Pending,
    Failed,
}

public sealed class VisitTimelineStep
{
    public required string TimeText { get; init; }
    public required string Text { get; init; }
    public required VisitStepState State { get; init; }
    public bool IsLast { get; set; }

    public bool IsDone => State == VisitStepState.Done;
    public bool IsPending => State == VisitStepState.Pending;
    public bool IsFailed => State == VisitStepState.Failed;
}
