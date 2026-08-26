using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.OperatorQueue.Models;

// The customer in the chair. The elapsed timer counts *up* against the estimate rather than down:
// counting up stays honest when a cut overruns, and elapsed is what eventually trains
// operator_avg_minutes. It deliberately never turns red or otherwise escalates — nagging a barber
// mid-haircut serves nobody and teaches them to ignore the screen.
public sealed class ServingCardItem : ObservableObject
{
    public Guid EntryId { get; init; }
    public Guid? OperatorId { get; init; }
    public Guid? ServiceId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string ServiceText { get; init; } = string.Empty;
    public DateTime? ServingAt { get; init; }

    // "of ~60m", or blank when the entry has no service to estimate against.
    public string EstimateText { get; init; } = string.Empty;
    public bool HasEstimate { get; init; }

    // One tappable line, not an always-open input with its own Save button — the note is an
    // occasional action and shouldn't spend a third of the card.
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
