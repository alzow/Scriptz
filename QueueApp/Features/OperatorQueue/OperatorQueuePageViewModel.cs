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
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
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
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IDispatcherTimer? _heartbeatTimer;
    private Guid _businessId;

    public ObservableCollection<OperatorColumn> Columns { get; } = new();
    public string BusinessName { get; set; } = "Queue";
    public bool IsLoading { get; set; }
    public bool IsEmpty => Columns.Count == 0 && !IsLoading;

    public ObservableCollection<ServiceResponse> WalkInServices { get; } = new();
    public ServiceResponse? SelectedWalkInService { get; set; }
    public bool IsWalkInServicesEmpty => WalkInServices.Count == 0;
    public OperatorResponse? PendingWalkInOperator { get; set; }
    public bool ShowWalkInServicePicker { get; set; }
    public bool IsAddingWalkIn { get; set; }

    public OperatorQueuePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueueService queueService,
        IBusinessService businessService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _queueService = queueService;
        _businessService = businessService;
        _operatorService = operatorService;
        _serviceOfferingsService = serviceOfferingsService;
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

            var services = await _serviceOfferingsService.GetActiveServicesAsync(_businessId);
            WalkInServices.Clear();
            foreach (var service in services)
                WalkInServices.Add(service);
            OnPropertyChanged(nameof(IsWalkInServicesEmpty));

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
    private void RequestAddWalkIn(OperatorResponse op)
    {
        PendingWalkInOperator = op;
        SelectedWalkInService = null;
        ShowWalkInServicePicker = true;
    }

    [RelayCommand]
    private void SelectWalkInService(ServiceResponse service) => SelectedWalkInService = service;

    [RelayCommand]
    private async Task ConfirmAddWalkInAsync()
    {
        if (PendingWalkInOperator is null || SelectedWalkInService is null) return;

        var op = PendingWalkInOperator;
        IsAddingWalkIn = true;
        try
        {
            await _queueService.AddWalkInAsync(
                _businessId, op.Id == Guid.Empty ? null : op.Id, "Walk-in", SelectedWalkInService.Id);

            ShowWalkInServicePicker = false;
            SelectedWalkInService = null;
            PendingWalkInOperator = null;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsAddingWalkIn = false;
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
    private async Task SaveProgressAsync(QueueEntryResponse entry)
    {
        entry.IsSavingProgress = true;
        try
        {
            await _queueService.SetQueueProgressAsync(entry.Id, entry.ProgressStatus);
            // No manual reload — realtime picks it up, same as every other mutation on this screen.
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            entry.IsSavingProgress = false;
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

    // Base HandleExceptionAsync only logs — this screen already has a popup service on hand
    // (used for the no-show confirm), so use it to surface real failures, most notably
    // start_serving's "all resources are currently busy" on a pooled business at capacity.
    // That's a normal operational state for staff, not a fault, so it deserves to actually be seen.
    protected override Task HandleExceptionAsync(Exception exception)
    {
        return _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
    }
}
