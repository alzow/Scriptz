using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.Welcome.Models;

public sealed class WelcomePanel : ObservableObject
{
    public required string NumberText { get; init; }
    public required string HeadlineText { get; init; }
    public required string BodyText { get; init; }
    public required string IllustrationSource { get; init; }

    public bool IsActive { get; set; }
}
