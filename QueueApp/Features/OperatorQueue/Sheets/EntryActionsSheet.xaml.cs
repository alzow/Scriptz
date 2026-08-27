using QueueApp.Shared.Templates.BottomSheet;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

public partial class EntryActionsSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<EntryAction> _completion = new();

    public string CustomerName { get; }
    public string Initials { get; }
    public string SubText { get; }
    public bool CanServe { get; }
    public bool CanReorder { get; }

    public Task<EntryAction> Completion => _completion.Task;

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
