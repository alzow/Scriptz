using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Features.OperatorQueue.Models;

public sealed class QueueRowItem : ObservableObject
{
    public Guid EntryId { get; init; }
    public Guid? OperatorId { get; init; }
    public Guid? ServiceId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
    public string JoinedAtText { get; init; } = string.Empty;

    public string PositionText { get; init; } = string.Empty;
    public bool ShowPosition { get; init; }
    public bool ShowServe { get; init; }
    public bool ShowAssign { get; init; }
    public bool ShowMarkCollected { get; init; }

    public string SubText { get; set; } = string.Empty;

    // The same progress_status the serving card edits, kept on the row so a message left for
    // someone who is still waiting reads off the board without opening their sheet.
    public string NoteText { get; init; } = string.Empty;
    public bool HasNote { get; init; }

    // The answers the customer gave on the way in, carried onto the row so the operator can open
    // them without a round trip. Empty whenever the service asked nothing, and for every walk-in
    // added at the counter — that sheet does not ask the service's questions.
    public IReadOnlyList<IntakeAnswer> IntakeAnswers { get; init; } = Array.Empty<IntakeAnswer>();
    public bool HasIntakeAnswers => IntakeAnswers.Count > 0;

    public bool SectionIsServing { get; init; }

    public bool IsBusy { get; set; }
    public bool IsEnabled => !IsBusy;
    public bool IsServeEnabled => !IsBusy && !SectionIsServing;

    public int WaitedMinutes =>
        (int)Math.Max(0, (DateTime.UtcNow - BoardConstants.AsUtc(JoinedAt)).TotalMinutes);

    public void RefreshWait()
    {
        var text = BuildSubText(ServiceName, WaitedMinutes);
        if (text != SubText)
            SubText = text;
    }

    public static string BuildSubText(string serviceName, int waitedMinutes)
    {
        var wait = $"waiting {waitedMinutes}m";
        return string.IsNullOrWhiteSpace(serviceName) ? wait : $"{serviceName} · {wait}";
    }

    // Empty rather than a dangling "joined" for the rows that carry no join time — the awaiting
    // collection list is built without one.
    public static string BuildJoinedText(string joinedAtText) =>
        string.IsNullOrWhiteSpace(joinedAtText) ? string.Empty : $"joined {joinedAtText}";

    public static string BuildReadySubText(string serviceName, int readyMinutes)
    {
        var ready = readyMinutes <= 0 ? "ready now" : $"ready {readyMinutes}m ago";
        return string.IsNullOrWhiteSpace(serviceName) ? ready : $"{serviceName} · {ready}";
    }
}
