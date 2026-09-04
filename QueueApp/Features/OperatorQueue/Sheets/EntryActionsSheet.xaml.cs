using QueueApp.Shared.Templates.BottomSheet;
using QueueApp.Features.OperatorQueue.Helpers;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

// Every move on a queue entry is made here, from a row that is otherwise only information. The one
// thing the sheet decides for itself is which action is the primary one, and it decides it from the
// stage the entry is in — which is what stops a waiting entry being completed without ever having
// been served.
public partial class EntryActionsSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<EntryActionResult> _completion = new();
    private readonly EntryAction _primaryAction;

    public string CustomerName { get; }
    public string Initials { get; }
    public string SubText { get; }

    public string PrimaryActionText { get; }
    public bool CanPrimary { get; }
    public bool ShowBusyNote => !CanPrimary;
    public string MoveActionText { get; }
    public bool CanReorder { get; }

    public bool HasAnswers { get; }
    public string AnswersSummaryText { get; }

    // Two-way bound to the note field, so it carries the edit out of the sheet rather than the
    // note it came in with.
    public string NoteText { get; set; }

    public string NoteHeaderText { get; }

    public Task<EntryActionResult> Completion => _completion.Task;

    public EntryActionsSheet() : this(null!, EmptyRequest(), false, string.Empty)
    {
    }

    public EntryActionsSheet(
        IQueuePopupService popups,
        EntrySheetRequest request,
        bool requiresCollection,
        string operatorNoun)
    {
        _popups = popups;

        CustomerName = request.CustomerName;
        Initials = request.Initials;
        SubText = request.SubText;

        _primaryAction = request.IsWaiting ? EntryAction.ServeNow : EntryAction.MarkDone;
        PrimaryActionText = OperatorQueueHelper.PrimaryActionText(request.Stage, requiresCollection);
        CanPrimary = !request.IsWaiting || request.CanStart;
        MoveActionText = OperatorQueueHelper.MoveActionText(request.IsInPool, operatorNoun);

        // Position is joined_at order, and the person in the chair has already left the line.
        CanReorder = request.IsWaiting;

        HasAnswers = request.Answers.Count > 0;
        AnswersSummaryText = OperatorQueueHelper.AnswersSummaryText(request.Answers.Count);

        NoteText = request.Note ?? string.Empty;
        NoteHeaderText = OperatorQueueHelper.NoteHeaderText(request.Note);

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(EntryActionResult.Dismissed);
    }

    private void OnPrimaryClicked(object? sender, EventArgs e) => Close(_primaryAction);
    private void OnMoveOperatorClicked(object? sender, EventArgs e) => Close(EntryAction.MoveToAnotherOperator);
    private void OnMoveToEndClicked(object? sender, EventArgs e) => Close(EntryAction.MoveToEndOfQueue);
    private void OnChangeServiceClicked(object? sender, EventArgs e) => Close(EntryAction.ChangeService);
    private void OnNoShowClicked(object? sender, EventArgs e) => Close(EntryAction.MarkNoShow);
    private void OnRemoveClicked(object? sender, EventArgs e) => Close(EntryAction.RemoveFromQueue);
    private void OnViewAnswersTapped(object? sender, EventArgs e) => Close(EntryAction.ViewAnswers);

    // Null rather than "" when the field is emptied: clearing a note is a real edit, and the board
    // writes null to take the message back off the customer's screen.
    private void OnSaveNoteClicked(object? sender, EventArgs e) =>
        Close(new EntryActionResult(
            EntryAction.SaveNote,
            string.IsNullOrWhiteSpace(NoteText) ? null : NoteText.Trim()));

    private void Close(EntryAction action) => Close(new EntryActionResult(action));

    private void Close(EntryActionResult result)
    {
        _completion.TrySetResult(result);
        _ = _popups.HideSheetAsync(this);
    }

    // The parameterless constructor the XAML previewer needs, given something to be about.
    private static EntrySheetRequest EmptyRequest() => new()
    {
        EntryId = Guid.Empty,
        CustomerName = string.Empty,
        Initials = string.Empty,
        ServiceName = string.Empty,
        SubText = string.Empty,
        WhenText = string.Empty,
    };
}
