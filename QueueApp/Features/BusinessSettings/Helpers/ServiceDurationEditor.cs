using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Features.BusinessSettings.Constants;
using QueueApp.Features.BusinessSettings.Models;

namespace QueueApp.Features.BusinessSettings.Helpers;

// The duration chips and the custom field behind them. Owns which chip is lit and what a saved
// duration resolves to, so the service view model only ever asks for the number.
public sealed class ServiceDurationEditor : ObservableObject
{
    public ObservableCollection<DurationChoice> Choices { get; } = new();

    public bool IsCustom { get; set; }
    public string CustomMinutesText { get; set; } = string.Empty;

    public ServiceDurationEditor()
    {
        foreach (var minutes in BusinessSettingsConstants.DurationChoices)
            Choices.Add(DurationChoice.Preset(minutes));

        Choices.Add(DurationChoice.Custom());
    }

    // A duration that is not one of the chips lights Custom and fills the field, so an existing
    // 20-minute service opens showing 20 rather than silently snapping to 15.
    public void Load(int minutes)
    {
        var preset = Choices.FirstOrDefault(c => c.Minutes == minutes);

        if (preset is not null)
        {
            Select(preset);
            return;
        }

        CustomMinutesText = minutes.ToString();
        Select(Choices.First(c => c.IsCustom));
    }

    public void Select(DurationChoice? choice)
    {
        if (choice is null)
            return;

        foreach (var candidate in Choices)
            candidate.IsSelected = ReferenceEquals(candidate, choice);

        IsCustom = choice.IsCustom;
    }

    // Null when nothing is chosen yet, or when the custom field holds something that isn't a
    // positive whole number of minutes.
    public int? ResolveMinutes()
    {
        var selected = Choices.FirstOrDefault(c => c.IsSelected);

        if (selected is null)
            return null;

        if (!selected.IsCustom)
            return selected.Minutes;

        return int.TryParse(CustomMinutesText, out var minutes) && minutes > 0
            ? minutes
            : null;
    }
}
