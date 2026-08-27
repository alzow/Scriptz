namespace QueueApp.Features.OperatorQueue.Models;

public sealed class AssignTargetItem
{
    public Guid? OperatorId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public string SubLabel { get; init; } = string.Empty;
    public bool ShowSoonestTag { get; set; }
    public bool IsSelectable { get; init; } = true;
    public bool ShowPresenceDot { get; init; }

    public bool IsPool { get; init; }

    public double RowOpacity => IsSelectable ? 1 : 0.4;
    public double SortWaitMinutes { get; init; }
}
