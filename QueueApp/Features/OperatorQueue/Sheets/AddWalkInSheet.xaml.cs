using QueueApp.Shared.Templates.BottomSheet;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Services.Popup;

namespace QueueApp.Features.OperatorQueue.Sheets;

public partial class AddWalkInSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<WalkInRequest?> _completion = new();
    private readonly Guid? _operatorId;
    private readonly int _aheadCount;
    private readonly double _waitMinutesAhead;

    public string TitleText { get; }
    public ObservableCollection<ServiceChoiceRow> Services { get; } = new();
    public bool IsEmpty => Services.Count == 0;
    public ICommand SelectCommand { get; }

    public string? CustomerName { get; set; }

    public ServiceChoiceRow? Selected { get; private set; }
    public bool CanAdd => Selected is not null && !_isAdding;
    public bool HasOutcome => Selected is not null;
    public string OutcomePositionText { get; private set; } = string.Empty;
    public string OutcomeTurnText { get; private set; } = string.Empty;

    private bool _isAdding;

    public Task<WalkInRequest?> Completion => _completion.Task;

    public AddWalkInSheet() : this(null!, string.Empty, null, 0, 0, [])
    {
    }

    public AddWalkInSheet(
        IQueuePopupService popups,
        string titleText,
        Guid? operatorId,
        int aheadCount,
        double waitMinutesAhead,
        IEnumerable<ServiceChoiceRow> services)
    {
        _popups = popups;
        TitleText = titleText;
        _operatorId = operatorId;
        _aheadCount = aheadCount;
        _waitMinutesAhead = waitMinutesAhead;

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

        foreach (var service in Services)
            service.IsSelected = ReferenceEquals(service, row);

        Selected = row;

        OutcomePositionText = $"Joins at position {_aheadCount + 1}";
        OutcomeTurnText = _waitMinutesAhead <= 0
            ? "Turn: next up"
            : $"Turn: about {DateTime.Now.AddMinutes(_waitMinutesAhead):HH:mm} · {_waitMinutesAhead:0} min";

        OnPropertyChanged(nameof(Selected));
        OnPropertyChanged(nameof(CanAdd));
        OnPropertyChanged(nameof(HasOutcome));
        OnPropertyChanged(nameof(OutcomePositionText));
        OnPropertyChanged(nameof(OutcomeTurnText));
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        if (Selected is null || _isAdding)
            return;

        _isAdding = true;
        OnPropertyChanged(nameof(CanAdd));

        var name = string.IsNullOrWhiteSpace(CustomerName) ? null : CustomerName.Trim();

        _completion.TrySetResult(new WalkInRequest(_operatorId, name, Selected.ServiceId));
        _ = _popups.HideSheetAsync(this);
    }
}
