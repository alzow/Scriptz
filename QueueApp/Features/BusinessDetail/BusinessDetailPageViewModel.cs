using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BusinessDetail;

public partial class BusinessDetailPageViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IAuthService _authService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Guid _businessId;

    public BusinessDetailPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IAuthService authService,
        IQueueRealtimeService realtimeService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _queueService = queueService;
        _authService = authService;
        _realtimeService = realtimeService;
    }

    public BusinessResponse? Business { get; set; }
    public ObservableCollection<QueueSummaryRow> QueueSummary { get; } = new();
    public MyQueueStatusResponse? MyStatus { get; set; }
    public decimal? MyWaitMinutes { get; set; }
    public bool IsInQueue => MyStatus != null;
    public bool IsBeingServed => MyStatus?.Status == "serving";
    public bool IsQueueMode => Business?.Mode == "queue";
    public bool IsBookingMode => Business?.Mode == "booking";
    public bool IsLoading { get; set; }
    public bool IsLeaving { get; set; }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue("businessId", out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("BusinessDetailPage requires a 'businessId' parameter.");

            IsLoading = true;
            Business = await _businessService.GetBusinessAsync(_businessId);
            Title = Business?.Name ?? "";
            if (IsQueueMode)
            {
                await RefreshQueueAsync();
                await RefreshMyStatusAsync();
            }
            IsLoading = false;

            await _realtimeService.SubscribeAsync(_businessId,
                async () => await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await RefreshQueueAsync();
                    await RefreshMyStatusAsync();
                }));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnDisappearingAsync()
    {
        await _realtimeService.UnsubscribeAsync();
    }

    private async Task RefreshQueueAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            var rows = await _queueService.GetQueueSummaryAsync(_businessId);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                QueueSummary.Clear();
                foreach (var row in rows)
                    QueueSummary.Add(row);
            });
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task RefreshMyStatusAsync()
    {
        MyStatus = await _queueService.GetMyQueueStatusAsync(_businessId);
        MyWaitMinutes = MyStatus is not null
            ? await _queueService.GetEntryWaitMinutesAsync(MyStatus.EntryId)
            : null;
    }

    [RelayCommand]
    private async Task JoinAsync(QueueSummaryRow? row)
    {
        if (row is null) return;

        row.IsJoining = true;
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            await _queueService.JoinQueueAsync(_businessId, row.OperatorId, Guid.Parse(userId));
            await RefreshQueueAsync();
            await RefreshMyStatusAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            row.IsJoining = false;
        }
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        if (MyStatus is null) return;

        IsLeaving = true;
        try
        {
            await _queueService.CancelEntryAsync(MyStatus.EntryId);
            MyStatus = null;
            MyWaitMinutes = null;
            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLeaving = false;
        }
    }
}
