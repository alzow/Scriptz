using QueueApp.Framework.Theming;

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

    // An operator who can't take this entry is dimmed by token, not by fading the row.
    public Color NameColor => IsSelectable ? ThemePalette.TextInk : ThemePalette.TextDim;
    public Color SubLabelColor => IsSelectable ? ThemePalette.TextMuted : ThemePalette.TextDim;
    public double SortWaitMinutes { get; init; }
}
