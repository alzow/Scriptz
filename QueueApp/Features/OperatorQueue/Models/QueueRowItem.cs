using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.OperatorQueue.Models;

// One waiting customer, in an operator's section or in the shared pool. Every display value is
// precomputed at map time — these bind inside item templates, which re-run on every recycle.
//
// Only the top row of a section carries an inline Serve, and pool rows carry Assign instead;
// nothing destructive lives here at all. Tapping the row opens the actions sheet.
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

    // "Haircut · waiting 14m" — re-stamped by the page tick as the wait grows.
    public string SubText { get; set; } = string.Empty;

    // Serve stays visible while somebody is in that barber's chair, but disabled: start_serving
    // would refuse it, and a live-looking button that errors on tap is worse than a dim one.
    public bool SectionIsServing { get; init; }

    // Double-tap guard: the button disables itself for the duration of the call. Completing or
    // starting the same entry twice skips a customer, with no undo.
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
