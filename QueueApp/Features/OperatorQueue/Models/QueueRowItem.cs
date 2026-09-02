using CommunityToolkit.Mvvm.ComponentModel;

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

    public string SubText { get; set; } = string.Empty;

    // The same progress_status the serving card edits, kept on the row so a message left for
    // someone who is still waiting reads off the board without opening their sheet.
    public string NoteText { get; init; } = string.Empty;
    public bool HasNote { get; init; }

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
}
