using MPowerKit.Popups;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

// Everything a row can do that isn't its one inline Serve. Rows on the board carry at most one
// live target each; this is where the rest lives, with the two destructive actions kept below a
// separator so neither can be reached by a slipped thumb aimed at Serve.
public partial class EntryActionsSheet : PopupPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<EntryAction> _completion = new();

    public string CustomerName { get; }
    public string Initials { get; }
    public string SubText { get; }
    public bool CanServe { get; }
    public bool CanReorder { get; }

    public Task<EntryAction> Completion => _completion.Task;

    // Parameterless ctor so the assembly-wide page scan in NavigationStartup can register the type
    // without needing to construct it. Sheets are always created directly, never navigated to.
    public EntryActionsSheet() : this(null!, string.Empty, string.Empty, string.Empty, false, false)
    {
    }

    public EntryActionsSheet(
        IQueuePopupService popups,
        string customerName,
        string initials,
        string subText,
        bool canServe,
        bool canReorder)
    {
        _popups = popups;
        CustomerName = customerName;
        Initials = initials;
        SubText = subText;
        CanServe = canServe;
        CanReorder = canReorder;

        InitializeComponent();
    }

    // Covers every way out that isn't one of the buttons below — a background tap, or the sheet
    // being torn down while it's open. TrySetResult, because the buttons close the sheet
    // themselves and will already have set a result by the time this runs.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(EntryAction.Dismissed);
    }

    private void OnServeClicked(object? sender, EventArgs e) => Close(EntryAction.ServeNow);
    private void OnMoveOperatorClicked(object? sender, EventArgs e) => Close(EntryAction.MoveToAnotherOperator);
    private void OnMoveToEndClicked(object? sender, EventArgs e) => Close(EntryAction.MoveToEndOfQueue);
    private void OnChangeServiceClicked(object? sender, EventArgs e) => Close(EntryAction.ChangeService);
    private void OnNoShowClicked(object? sender, EventArgs e) => Close(EntryAction.MarkNoShow);
    private void OnRemoveClicked(object? sender, EventArgs e) => Close(EntryAction.RemoveFromQueue);

    private void Close(EntryAction action)
    {
        _completion.TrySetResult(action);
        _ = _popups.HideSheetAsync(this);
    }
}
