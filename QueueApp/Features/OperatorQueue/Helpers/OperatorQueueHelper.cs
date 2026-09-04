using QueueApp.Features.OperatorQueue.Models;

namespace QueueApp.Features.OperatorQueue.Helpers;

public static class OperatorQueueHelper
{
    // One action reads as the next thing to do, and which one it is follows from where the entry
    // already is. A service that hands something back ends in collection rather than done, so the
    // button says the state it is actually about to write.
    public static string PrimaryActionText(EntryStage stage, bool requiresCollection) => stage switch
    {
        EntryStage.Waiting => BoardConstants.StartServingAction,
        _ => requiresCollection ? BoardConstants.ReadyForCollectionAction : BoardConstants.MarkDoneAction,
    };

    public static string MoveActionText(bool isInPool, string operatorNoun) => isInPool
        ? $"Assign to a {operatorNoun.ToLowerInvariant()}"
        : $"Move to another {operatorNoun.ToLowerInvariant()}";

    public static string AssignHeaderText(bool isInPool) => isInPool
        ? BoardConstants.AssignHeader
        : BoardConstants.MoveHeader;

    // Says what is there rather than naming a verb: every other row in the sheet changes the entry,
    // and this one only opens what the customer already wrote. The count is the point — it tells the
    // operator whether opening it is worth the tap.
    public static string AnswersSummaryText(int answerCount) => answerCount == 1
        ? BoardConstants.OneAnswerSummary
        : string.Format(BoardConstants.ManyAnswersSummary, answerCount);

    public static string NoteHeaderText(string? note) =>
        string.IsNullOrWhiteSpace(note) ? BoardConstants.LeaveNoteHeader : BoardConstants.NoteHeader;
}
