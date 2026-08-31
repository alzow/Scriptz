namespace QueueApp.Features.Settings.Models;

public class DurationChipOption
{
    public int Minutes { get; }
    public string Label { get; }
    public bool IsCustom { get; }
    public bool IsSelected { get; set; }

    public DurationChipOption(int minutes, string label, bool isCustom = false)
    {
        Minutes = minutes;
        Label = label;
        IsCustom = isCustom;
    }
}
