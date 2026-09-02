namespace QueueApp.Features.Flow.Visit.Models;

public enum VisitStepState
{
    Done,
    Pending,
    Failed,
}

public sealed class VisitTimelineStep
{
    public required string Text { get; init; }

    // "Today · 20:34" — the day is carried because a queue joined last night and a slot booked for
    // next Sunday sat in the same list reading 20:34 and 16:00, as if minutes apart.
    public required string MomentText { get; init; }

    public required VisitStepState State { get; init; }
    public bool IsLast { get; set; }

    public bool HasMoment => !string.IsNullOrEmpty(MomentText);

    public bool IsDone => State == VisitStepState.Done;
    public bool IsPending => State == VisitStepState.Pending;
    public bool IsFailed => State == VisitStepState.Failed;
}
