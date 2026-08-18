using System.Collections.ObjectModel;
using System.Threading;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BusinessDetail;

public partial class BusinessDetailPageViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Guid _businessId;

    public BusinessDetailPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IQueueRealtimeService realtimeService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _queueService = queueService;
        _realtimeService = realtimeService;
    }

    public BusinessResponse? Business { get; set; }
    public ObservableCollection<QueueSummaryRow> QueueSummary { get; } = new();
    public bool IsQueueMode => Business?.Mode == "queue";
    public bool IsBookingMode => Business?.Mode == "booking";
    public bool IsLoading { get; set; }

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
                await RefreshQueueAsync();
            IsLoading = false;

            await _realtimeService.SubscribeAsync(_businessId,
                async () => await MainThread.InvokeOnMainThreadAsync(RefreshQueueAsync));
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
}
