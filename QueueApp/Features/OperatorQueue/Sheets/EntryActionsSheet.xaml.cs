using QueueApp.Shared.Templates.BottomSheet;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

public partial class EntryActionsSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<EntryActionResult> _completion = new();

    public string CustomerName { get; }
    public string Initials { get; }
    public string SubText { get; }
    public bool CanServe { get; }
    public bool CanReorder { get; }
    public bool HasIntakeAnswers { get; }

    // Two-way bound to the note field, so it carries the edit out of the sheet rather than the
    // note it came in with.
    public string NoteText { get; set; }

    public string NoteHeaderText { get; }

    public Task<EntryActionResult> Completion => _completion.Task;

    public EntryActionsSheet() : this(null!, string.Empty, string.Empty, string.Empty, false, false)
    {
    }

    public EntryActionsSheet(
        IQueuePopupService popups,
        string customerName,
        string initials,
        string subText,
        bool canServe,
        bool canReorder,
        string? note = null,
        bool hasIntakeAnswers = false)
    {
        _popups = popups;
        CustomerName = customerName;
        Initials = initials;
        SubText = subText;
        CanServe = canServe;
        CanReorder = canReorder;
        HasIntakeAnswers = hasIntakeAnswers;
        NoteText = note ?? string.Empty;
        NoteHeaderText = string.IsNullOrWhiteSpace(note) ? "LEAVE A NOTE" : "NOTE";

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(EntryActionResult.Dismissed);
    }

    private void OnServeClicked(object? sender, EventArgs e) => Close(EntryAction.ServeNow);
    private void OnMoveOperatorClicked(object? sender, EventArgs e) => Close(EntryAction.MoveToAnotherOperator);
    private void OnMoveToEndClicked(object? sender, EventArgs e) => Close(EntryAction.MoveToEndOfQueue);
    private void OnChangeServiceClicked(object? sender, EventArgs e) => Close(EntryAction.ChangeService);
    private void OnNoShowClicked(object? sender, EventArgs e) => Close(EntryAction.MarkNoShow);
    private void OnRemoveClicked(object? sender, EventArgs e) => Close(EntryAction.RemoveFromQueue);
    private void OnViewAnswersClicked(object? sender, EventArgs e) => Close(EntryAction.ViewAnswers);

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
}
