using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.OperatorQueue.Models;

// A service row in the walk-in and change-service sheets. Selection is green, not purple:
// picking a service is a commit, and purple on this screen is reserved for the unassigned pool.
//
// The colours are properties rather than converter output because these bind inside an item
// template, which re-runs on every bind and every recycle.
public sealed class ServiceChoiceRow : ObservableObject
{
    public Guid ServiceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string MetaText { get; init; } = string.Empty;
    public int EstMinutes { get; init; }

    public bool IsSelected { get; set; }

    // Brush, not Color: Border.Stroke is Brush-typed and a binding won't convert for us.
    public Brush BorderBrush => IsSelected ? BoardPalette.GreenStroke : BoardPalette.LineStroke;

    public Color FillColor => IsSelected
        ? Color.FromArgb("#1839FF7A")
        : Color.FromArgb("#1C222D");

    public Color NameColor => IsSelected
        ? Color.FromArgb("#39FF7A")
        : Color.FromArgb("#F2F4F7");
}
