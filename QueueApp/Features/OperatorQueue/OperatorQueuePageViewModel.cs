using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
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
    private readonly IBusinessService _businessService;
    private readonly IOperatorService _operatorService;
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
        IBusinessService businessService,
        IOperatorService operatorService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _queueService = queueService;
        _businessService = businessService;
        _operatorService = operatorService;
        _realtimeService = realtimeService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj)
                ? (Guid)idObj
                : await _businessService.GetOwnedBusinessIdAsync();

            var business = await _businessService.GetBusinessAsync(_businessId);
            BusinessName = business?.Name ?? "Queue";

            await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        await _realtimeService.SubscribeAsync("business_id", _businessId.ToString(),
                async () => await MainThread.InvokeOnMainThreadAsync(LoadQueueAsync));
        StartHeartbeat();
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
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

            var operators = await _operatorService.GetOperatorsAsync(_businessId);
            var active = await _queueService.GetActiveEntriesAsync(_businessId);

            var rebuilt = new List<OperatorColumn>();

            foreach (var op in operators.OrderBy(o => o.SortOrder))
            {
                var column = new OperatorColumn { Operator = op };
                column.Serving = active.FirstOrDefault(e => e.OperatorId == op.Id && e.Status == "serving");
                foreach (var entry in active.Where(e => e.OperatorId == op.Id && e.Status == "waiting"))
                    column.Waiting.Add(entry);
                rebuilt.Add(column);
            }

            var unassigned = active.Where(e => e.OperatorId is null).ToList();
            if (unassigned.Count > 0)
            {
                var column = new OperatorColumn
                {
                    Operator = new OperatorResponse { DisplayName = "Any available" },
                };
                column.Serving = unassigned.FirstOrDefault(e => e.Status == "serving");
                foreach (var entry in unassigned.Where(e => e.Status == "waiting"))
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
        _ = _businessService.HeartbeatAsync(_businessId);

        _heartbeatTimer = Application.Current!.Dispatcher.CreateTimer();
        _heartbeatTimer.Interval = TimeSpan.FromMinutes(2);
        _heartbeatTimer.Tick += async (_, _) => await _businessService.HeartbeatAsync(_businessId);
        _heartbeatTimer.Start();
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.BusinessSettingsPage);
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
