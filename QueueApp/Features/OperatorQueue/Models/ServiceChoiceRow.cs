using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.OperatorQueue.Models;

public sealed class ServiceChoiceRow : ObservableObject
{
    public Guid ServiceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string MetaText { get; init; } = string.Empty;
    public int EstMinutes { get; init; }

    public bool IsSelected { get; set; }

    public Brush BorderBrush => IsSelected ? BoardPalette.GreenStroke : BoardPalette.LineStroke;

    public Color FillColor => IsSelected
        ? (Color)Application.Current!.Resources["GreenSelectedFill"]
        : (Color)Application.Current!.Resources["Surface"];

    public Color NameColor => IsSelected
        ? (Color)Application.Current!.Resources["Green"]
        : (Color)Application.Current!.Resources["TextPrimary"];
}
