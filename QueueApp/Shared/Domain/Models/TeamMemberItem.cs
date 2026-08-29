using QueueApp.Framework.Theming;

namespace QueueApp.Shared.Domain.Models;

// Landing-page team strip. Avatars stay neutral: purple is this system's informational colour and
// identity is not a status, so presence is carried by the dot and by how bright the name reads.
public sealed class TeamMemberItem
{
    public string Initials { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SubLabel { get; init; } = string.Empty;
    public bool ShowSubLabel { get; init; }
    public bool IsOnShift { get; init; }

    // Off shift used to be a 0.4 opacity on the whole tile. On light that fades the name toward
    // the page and reads as broken rather than receded, so the name steps down a token instead.
    public Color NameColor => IsOnShift ? ThemePalette.TextInk : ThemePalette.TextDim;
    public Color SubLabelColor => IsOnShift ? ThemePalette.TextMuted : ThemePalette.TextDim;
}
