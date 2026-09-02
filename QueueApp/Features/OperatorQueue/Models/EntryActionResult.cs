namespace QueueApp.Features.OperatorQueue.Models;

// The note travels back with the action because SaveNote is the one entry action that carries a
// value the sheet collected rather than a decision the board already knows how to make.
public sealed record EntryActionResult(EntryAction Action, string? Note = null)
{
    public static readonly EntryActionResult Dismissed = new(EntryAction.Dismissed);
}
