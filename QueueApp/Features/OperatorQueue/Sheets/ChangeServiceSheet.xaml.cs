using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Popups;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

// Correcting the service on an entry that's already in the queue. One tap commits — there's no
// second confirm, because changing a service is cheap to undo by changing it again.
public partial class ChangeServiceSheet : PopupPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<Guid?> _completion = new();

    public string TitleText { get; }
    public ObservableCollection<ServiceChoiceRow> Services { get; } = new();
    public ICommand SelectCommand { get; }

    public Task<Guid?> Completion => _completion.Task;

    // Parameterless ctor so the assembly-wide page scan in NavigationStartup can register the type.
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
