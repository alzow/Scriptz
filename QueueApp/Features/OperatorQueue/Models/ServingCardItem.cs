using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.OperatorQueue.Models;

public sealed class ServingCardItem : ObservableObject
{
    public Guid EntryId { get; init; }
    public Guid? OperatorId { get; init; }
    public Guid? ServiceId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string ServiceText { get; init; } = string.Empty;
    public DateTime? ServingAt { get; init; }

    public string EstimateText { get; init; } = string.Empty;
    public bool HasEstimate { get; init; }

    public string NoteText { get; set; } = string.Empty;
    public bool HasNote { get; set; }

    public string ElapsedText { get; set; } = "00:00";

    public bool IsBusy { get; set; }
    public bool IsEnabled => !IsBusy;

    public void RefreshElapsed()
    {
        if (ServingAt is not { } startedAt)
            return;

        var elapsed = DateTime.UtcNow - BoardConstants.AsUtc(startedAt);
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        var text = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";

        if (text != ElapsedText)
            ElapsedText = text;
    }
}
