using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Framework.Theming;

namespace QueueApp.Features.OperatorQueue.Models;

public sealed class BoardSection : ObservableObject
{
    public Guid OperatorId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public int SortOrder { get; init; }

    public bool IsOnShift { get; init; }
    public bool IsOffShift => !IsOnShift;

    public ServingCardItem? Serving { get; init; }
    public bool HasServing => Serving is not null;
    public ObservableCollection<QueueRowItem> Waiting { get; } = new();

    public bool IsExpanded { get; init; }
    public bool IsCollapsed => IsOnShift && !IsExpanded;

    public string StatusText { get; init; } = string.Empty;

    public Color StatusColor { get; init; } = Colors.Transparent;

    // Off shift steps the operator's name down a token rather than fading the whole card: the
    // rows inside it still have to be readable at a glance from behind the counter.
    public Color NameColor => IsOnShift ? ThemePalette.TextInk : ThemePalette.TextDim;

    public bool IsTogglingShift { get; set; }
    public bool IsShiftToggleEnabled => !IsTogglingShift;
}
