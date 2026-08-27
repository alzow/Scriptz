using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

public partial class ChangeServiceSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<Guid?> _completion = new();

    public string TitleText { get; }
    public ObservableCollection<ServiceChoiceRow> Services { get; } = new();
    public ICommand SelectCommand { get; }

    public Task<Guid?> Completion => _completion.Task;

    public ChangeServiceSheet() : this(null!, string.Empty, [])
    {
    }

    public ChangeServiceSheet(IQueuePopupService popups, string titleText, IEnumerable<ServiceChoiceRow> services)
    {
        _popups = popups;
        TitleText = titleText;

        foreach (var service in services)
            Services.Add(service);

        SelectCommand = new RelayCommand<ServiceChoiceRow>(Select);

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(null);
    }

    private void Select(ServiceChoiceRow? row)
    {
        if (row is null)
            return;

        _completion.TrySetResult(row.ServiceId);
        _ = _popups.HideSheetAsync(this);
    }
}
