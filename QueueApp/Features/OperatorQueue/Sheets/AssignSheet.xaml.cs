using QueueApp.Shared.Templates.BottomSheet;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

public partial class AssignSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<AssignSheetResult> _completion = new();

    public string CustomerName { get; }
    public string Initials { get; }
    public string SubText { get; }
    public string PromptText { get; }
    public bool ShowNoShow { get; }
    public ObservableCollection<AssignTargetItem> Targets { get; } = new();
    public ICommand PickCommand { get; }

    public Task<AssignSheetResult> Completion => _completion.Task;

    public AssignSheet() : this(null!, string.Empty, string.Empty, string.Empty, string.Empty, false, [])
    {
    }

    public AssignSheet(
        IQueuePopupService popups,
        string customerName,
        string initials,
        string subText,
        string promptText,
        bool showNoShow,
        IEnumerable<AssignTargetItem> targets)
    {
        _popups = popups;
        CustomerName = customerName;
        Initials = initials;
        SubText = subText;
        PromptText = promptText;
        ShowNoShow = showNoShow;

        foreach (var target in targets)
            Targets.Add(target);

        PickCommand = new RelayCommand<AssignTargetItem>(Pick);

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(AssignSheetResult.Dismissed);
    }

    private void Pick(AssignTargetItem? target)
    {
        if (target is null || !target.IsSelectable)
            return;

        Close(new AssignSheetResult(true, target.OperatorId, false));
    }

    private void OnNoShowClicked(object? sender, EventArgs e)
        => Close(new AssignSheetResult(false, null, true));

    private void Close(AssignSheetResult result)
    {
        _completion.TrySetResult(result);
        _ = _popups.HideSheetAsync(this);
    }
}
