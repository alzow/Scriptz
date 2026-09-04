using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Intake.Models;

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

    // The answers the customer gave on the way in, carried onto the row so the operator can open
    // them without a round trip. Empty whenever the service asked nothing, and for every walk-in
    // added at the counter — that sheet does not ask the service's questions.
    public IReadOnlyList<IntakeAnswer> IntakeAnswers { get; init; } = Array.Empty<IntakeAnswer>();
    public bool HasIntakeAnswers => IntakeAnswers.Count > 0;

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
