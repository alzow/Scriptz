using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Api.Queue;
using ScriptzApp.Services.Api.Queue.Models;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Popup;
using ScriptzApp.Services.Realtime;
using ScriptzApp.Services.Storage;

namespace ScriptzApp.Features.Queue;

public partial class BarberQueuePageViewModel : BaseViewModel
{
    private readonly IQueueService _queueService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IScriptzPopupService _popupService;
    private IDispatcherTimer? _heartbeatTimer;

    // TEMP: hard-coded until auth/business selection is wired up. Set to your seeded test business id.
    private readonly Guid _businessId = new Guid("0637f5ef-c7fa-46dc-b4e5-b814f2d7d3bf");

    public ObservableCollection<OperatorColumn> Columns { get; } = new();

    public BarberQueuePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueueService queueService,
        IQueueRealtimeService realtimeService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _queueService = queueService;
        _realtimeService = realtimeService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await _realtimeService.SubscribeAsync(_businessId,
            async () => await MainThread.InvokeOnMainThreadAsync(LoadQueueAsync));
        StartHeartbeat();
    }

    public override async Task OnDisappearingAsync()
    {
        _heartbeatTimer?.Stop();
        _heartbeatTimer = null;
        await _realtimeService.UnsubscribeAsync();
    }

    private async Task LoadQueueAsync()
    {
        var operators = await _queueService.GetOperatorsAsync(_businessId);
        var waiting = await _queueService.GetWaitingAsync(_businessId);

        Columns.Clear();

        foreach (var op in operators.OrderBy(o => o.SortOrder))
        {
            var column = new OperatorColumn { Operator = op };
            foreach (var entry in waiting.Where(e => e.OperatorId == op.Id))
                column.Waiting.Add(entry);
            Columns.Add(column);
        }

        var unassigned = waiting.Where(e => e.OperatorId is null).ToList();
        if (unassigned.Count > 0)
        {
            var column = new OperatorColumn
            {
                Operator = new OperatorResponse { DisplayName = "Any barber" },
            };
            foreach (var entry in unassigned)
                column.Waiting.Add(entry);
            Columns.Add(column);
        }
    }

    private void StartHeartbeat()
    {
        _ = _queueService.HeartbeatAsync(_businessId);

        _heartbeatTimer = Application.Current!.Dispatcher.CreateTimer();
        _heartbeatTimer.Interval = TimeSpan.FromMinutes(2);
        _heartbeatTimer.Tick += async (_, _) => await _queueService.HeartbeatAsync(_businessId);
        _heartbeatTimer.Start();
    }

    [RelayCommand]
    private async Task AddWalkInAsync(OperatorResponse op)
    {
            await _queueService.AddWalkInAsync(_businessId, op.Id == Guid.Empty ? null : op.Id, "Walk-in");
            await LoadQueueAsync();
    }

    [RelayCommand]
    private async Task ServeAsync(QueueEntryResponse entry)
    {

            await _queueService.StartServingAsync(entry.Id);
            await LoadQueueAsync();

    }

    [RelayCommand]
    private async Task DoneAsync(QueueEntryResponse entry)
    {

            await _queueService.CompleteAsync(entry.Id);
            await LoadQueueAsync();

    }

    [RelayCommand]
    private async Task NoShowAsync(QueueEntryResponse entry)
    {
        var confirmed = await _popupService.ShowConfirmAsync("No-show", $"Mark {entry.CustomerName} as a no-show?");
        if (!confirmed) return;


            await _queueService.NoShowAsync(entry.Id);
            await LoadQueueAsync();
    }
}
