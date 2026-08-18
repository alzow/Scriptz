using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.OperatorQueue;

public partial class OperatorQueuePageViewModel : BaseViewModel
{
    private readonly IQueueService _queueService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IDispatcherTimer? _heartbeatTimer;
    private Guid _businessId;

    public ObservableCollection<OperatorColumn> Columns { get; } = new();
    public string BusinessName { get; set; } = "Queue";
    public bool IsLoading { get; set; }
    public bool IsEmpty => Columns.Count == 0 && !IsLoading;

    public OperatorQueuePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueueService queueService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService)
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

            _businessId = parameters is not null && parameters.TryGetValue("businessId", out var idObj)
                ? (Guid)idObj
                : await _queueService.GetOwnedBusinessIdAsync();

            var business = await _queueService.GetBusinessAsync(_businessId);
            BusinessName = business?.Name ?? "Queue";

            await LoadQueueAsync();

            await _realtimeService.SubscribeAsync(_businessId,
                async () => await MainThread.InvokeOnMainThreadAsync(LoadQueueAsync));
            StartHeartbeat();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnDisappearingAsync()
    {
        _heartbeatTimer?.Stop();
        _heartbeatTimer = null;
        await _realtimeService.UnsubscribeAsync();
    }

    private async Task LoadQueueAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            IsLoading = true;

            var operators = await _queueService.GetOperatorsAsync(_businessId);
            var waiting = await _queueService.GetWaitingAsync(_businessId);

            var rebuilt = new List<OperatorColumn>();

            foreach (var op in operators.OrderBy(o => o.SortOrder))
            {
                var column = new OperatorColumn { Operator = op };
                foreach (var entry in waiting.Where(e => e.OperatorId == op.Id))
                    column.Waiting.Add(entry);
                rebuilt.Add(column);
            }

            var unassigned = waiting.Where(e => e.OperatorId is null).ToList();
            if (unassigned.Count > 0)
            {
                var column = new OperatorColumn
                {
                    Operator = new OperatorResponse { DisplayName = "Any available" },
                };
                foreach (var entry in unassigned)
                    column.Waiting.Add(entry);
                rebuilt.Add(column);
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Columns.Clear();
                foreach (var column in rebuilt)
                    Columns.Add(column);
            });
        }
        finally
        {
            IsLoading = false;
            _loadLock.Release();
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
        var column = Columns.FirstOrDefault(c => c.Operator == op);
        if (column is not null)
            column.IsAddingWalkIn = true;
        try
        {
            await _queueService.AddWalkInAsync(_businessId, op.Id == Guid.Empty ? null : op.Id, "Walk-in");
            // await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            if (column is not null)
                column.IsAddingWalkIn = false;
        }
    }

    [RelayCommand]
    private async Task ServeAsync(QueueEntryResponse entry)
    {
        entry.IsServing = true;
        try
        {
            await _queueService.StartServingAsync(entry.Id);
            // await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            entry.IsServing = false;
        }
    }

    [RelayCommand]
    private async Task DoneAsync(QueueEntryResponse entry)
    {
        entry.IsCompleting = true;
        try
        {
            await _queueService.CompleteAsync(entry.Id);
            // await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            entry.IsCompleting = false;
        }
    }

    [RelayCommand]
    private async Task NoShowAsync(QueueEntryResponse entry)
    {
        entry.IsMarkingNoShow = true;
        try
        {
            var confirmed = await _popupService.ShowConfirmAsync("No-show", $"Mark {entry.CustomerName} as a no-show?");
            if (!confirmed)
            {
                entry.IsMarkingNoShow = false;
                return;
            }

            await _queueService.NoShowAsync(entry.Id);
            // await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            entry.IsMarkingNoShow = false;
        }
    }
}
