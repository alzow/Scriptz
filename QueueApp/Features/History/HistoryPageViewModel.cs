using System.Collections.ObjectModel;
using System.Diagnostics;
using MPowerKit.Navigation.Interfaces;
using Newtonsoft.Json;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;

namespace QueueApp.Features.History;

public class HistoryPageViewModel : BaseViewModel
{
    private readonly IQueueService _queueService;
    private readonly IAuthService _authService;

    public HistoryPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueueService queueService,
        IAuthService authService)
        : base(navigationService, secureStorageService)
    {
        _queueService = queueService;
        _authService = authService;
    }

    public ObservableCollection<VisitResponse> Visits { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsEmpty => Visits.Count == 0 && !IsLoading;

    private bool _isLoaded;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        // OnLoadedAsync already fetches on first navigation; avoid a duplicate race on initial appear.
        if (!_isLoaded)
            return;

        await RefreshVisitsAsync();
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        await base.OnLoadedAsync(parameters);
        _isLoaded = true;
        await RefreshVisitsAsync();
    }

    private async Task RefreshVisitsAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;

            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            var visits = await _queueService.GetMyVisitsAsync(Guid.Parse(userId));
            Debug.WriteLine($"[History] fetched {visits.Count} visits for user {JsonConvert.SerializeObject(visits)}");

            Visits.Clear();
            foreach (var visit in visits)
                Visits.Add(visit);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
            _refreshLock.Release();
        }
    }
}
