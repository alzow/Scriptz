namespace QueueApp.Features.BusinessDetail.Models;

// Landing-page team strip. Avatars stay neutral: purple is this system's informational colour and
// identity is not a status, so presence is carried by the dot and the opacity alone.
public sealed class TeamMemberItem
{
    public string Initials { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SubLabel { get; init; } = string.Empty;
    public bool ShowSubLabel { get; init; }
    public bool IsOnShift { get; init; }
    public double RowOpacity { get; init; }
}
