using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.CategoryPicker.Helpers;
using QueueApp.Features.CategoryPicker.Models;
using QueueApp.Features.CategoryPicker.Sheets;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Location;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.CategoryPicker;

public partial class CategoryPickerPageViewModel : BaseViewModel
{
    private static readonly TimeSpan CacheFreshWindow = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MinimumResolvingDisplay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ResolvingTimeout = TimeSpan.FromSeconds(4);
    private const int UpcomingBookingsShown = 3;

    public IReadOnlyList<ServiceCategory> Categories { get; } = CategoryCatalog.All.Where(c => c.Available).ToList();
    public bool IsLoading { get; set; }
    public bool IsRefreshing { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public ServiceCategory? SelectedCategory { get; set; }
    public MyActiveQueueEntryResponse? ActiveEntry { get; set; }
    public bool IsLeavingQueue { get; set; }
    public LocationBarState LocationState { get; set; } = LocationBarState.Resolving;
    public string LocationBarText { get; set; } = "Finding your location…";
    public ObservableCollection<BrowseBusinessSummaryResponse> Businesses { get; } = new();
    public ObservableCollection<UpcomingBookingResponse> UpcomingBookings { get; } = new();
    public ObservableCollection<FrequentBusinessItem> FrequentBusinesses { get; } = new();

    public bool HasActiveEntry => ActiveEntry is not null;
    public bool HasUpcomingBookings => UpcomingBookings.Count > 0;
    public bool HasFrequentBusinesses => FrequentBusinesses.Count > 0;
    public bool IsBusinessesEmpty => Businesses.Count == 0 && !IsLoading;

    private List<BrowseBusinessSummaryResponse> _allBusinesses = new();
    private Guid _customerId;
    private double? _customerLatitude;
    private double? _customerLongitude;
    private CustomerLocation? _lastKnownLocation;
    private readonly SemaphoreSlim _realtimeLock = new(1, 1);
    private bool _isVisible;
    private bool _hasAppeared;

    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly ILocationService _locationService;

    public CategoryPickerPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IBookingService bookingService,
        IAuthService authService,
        IQueuePopupService popupService,
        IQueueRealtimeService realtimeService,
        ILocationService locationService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _queueService = queueService;
        _bookingService = bookingService;
        _authService = authService;
        _popupService = popupService;
        _realtimeService = realtimeService;
        _locationService = locationService;

        PropertyChanged += OnAnyPropertyChanged;
    }


    public void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(SearchText))
                ApplyBusinessFilter();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            var userId = await _authService.GetUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
                _customerId = Guid.Parse(userId);

            var cached = await _locationService.GetCachedLocationAsync();
            var cacheIsFresh = false;
            if (cached is not null)
            {
                _customerLatitude = cached.Latitude;
                _customerLongitude = cached.Longitude;
                _lastKnownLocation = cached;
                ApplyLocationState(cached.IsCoarse ? LocationBarState.Coarse : LocationBarState.Resolved, cached.Label);
                cacheIsFresh = DateTimeOffset.UtcNow - cached.ResolvedAt < CacheFreshWindow;
            }

            await LoadAsync();

            // Not awaited: a live fix is a permission prompt on a cold install and up to twelve
            // seconds of GPS after that, and the dashboard is already on screen from the cached
            // location. A fresh cache renders immediately above and only refreshes quietly in the
            // background here; a stale or absent one shows the animated Resolving state itself.
            _ = ResolveLocationAsync(showAnimation: !cacheIsFresh);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void ApplyLocationState(LocationBarState state, string text)
    {
        LocationState = state;
        LocationBarText = text;
    }

    // The bar's own patience, separate from ILocationService's 12s GPS timeout: past 4s of
    // Resolving the bar gives up and reads Failed, while the underlying fix (and, if it lands,
    // the businesses list) still updates quietly once it actually completes.
    public async Task ResolveLocationAsync(bool showAnimation)
    {
        if (showAnimation)
            ApplyLocationState(LocationBarState.Resolving, "Finding your location…");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var refreshTask = _locationService.RefreshLocationAsync();

        if (showAnimation)
        {
            var completedInTime = await Task.WhenAny(refreshTask, Task.Delay(ResolvingTimeout)) == refreshTask;
            if (!completedInTime)
            {
                ApplyLocationState(LocationBarState.Failed, "Couldn't find you — tap to set");
                _ = ApplyLocationResultWhenReadyAsync(refreshTask);
                return;
            }

            var elapsed = stopwatch.Elapsed;
            if (elapsed < MinimumResolvingDisplay)
                await Task.Delay(MinimumResolvingDisplay - elapsed);
        }

        await ApplyLocationResultWhenReadyAsync(refreshTask);
    }

    public async Task ApplyLocationResultWhenReadyAsync(Task<LocationResolution> refreshTask)
    {
        try
        {
            await ApplyLocationResultAsync(await refreshTask);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task ApplyLocationResultAsync(LocationResolution result)
    {
        switch (result.Outcome)
        {
            case LocationOutcome.Resolved:
            case LocationOutcome.Coarse:
                var location = result.Location!;
                var moved = location.HasMovedFrom(_customerLatitude, _customerLongitude);
                _customerLatitude = location.Latitude;
                _customerLongitude = location.Longitude;
                _lastKnownLocation = location;
                ApplyLocationState(
                    result.Outcome == LocationOutcome.Coarse ? LocationBarState.Coarse : LocationBarState.Resolved,
                    location.Label);
                if (moved)
                    await LoadAsync();
                break;
            case LocationOutcome.Denied:
                ApplyLocationState(LocationBarState.Denied, "Set your location");
                break;
            case LocationOutcome.Failed:
                ApplyLocationState(LocationBarState.Failed, "Couldn't find you — tap to set");
                break;
        }
    }

    [RelayCommand]
    public async Task OpenLocationSheetAsync()
    {
        try
        {
            var sheet = new LocationSheet(
                _locationService,
                _popupService,
                _lastKnownLocation?.Label ?? string.Empty,
                _lastKnownLocation is not null ? FormatUpdatedAgo(_lastKnownLocation.ResolvedAt) : string.Empty,
                hasCurrentLocation: _lastKnownLocation is not null,
                isDenied: LocationState == LocationBarState.Denied,
                isRetryingAfterFailure: LocationState == LocationBarState.Failed);

            await _popupService.ShowSheetAsync(sheet);
            var result = await sheet.Completion;

            if (result is not null)
                await ApplyLocationResultAsync(result);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public static string FormatUpdatedAgo(DateTimeOffset resolvedAt)
    {
        var elapsed = DateTimeOffset.UtcNow - resolvedAt;

        if (elapsed < TimeSpan.FromMinutes(1))
            return "Updated just now";
        if (elapsed < TimeSpan.FromHours(1))
            return $"Updated {Plural((int)elapsed.TotalMinutes, "minute")} ago";
        if (elapsed < TimeSpan.FromDays(1))
            return $"Updated {Plural((int)elapsed.TotalHours, "hour")} ago";

        return $"Updated {Plural((int)elapsed.TotalDays, "day")} ago";
    }

    private static string Plural(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        try
        {
            _isVisible = true;

            if (_customerId == Guid.Empty)
            {
                var userId = await _authService.GetUserIdAsync();
                if (!string.IsNullOrEmpty(userId))
                    _customerId = Guid.Parse(userId);
            }

            // Coming back from a page that was over the tabs — a booking or a queue join, most of
            // the time. The feed was released while that page was up, so the ticket and the upcoming
            // list are as old as the moment the dashboard left, and the thing the customer just did
            // is exactly what is missing from them. The rest of the dashboard (businesses nearby,
            // frequently visited) does not go stale on a detour, so this refreshes the two live
            // things rather than reloading the whole screen.
            //
            // The first Appearing runs before Loaded on Android, and Loaded does the full load, so
            // there is nothing to refresh on that pass.
            if (_hasAppeared)
            {
                // RefreshActiveEntryAsync resubscribes once it knows whether a ticket is held, since
                // that decides whether the feed is scoped to the business or to the customer.
                var entryTask = RefreshActiveEntryAsync();
                var bookingsTask = RefreshUpcomingBookingsAsync();
                await entryTask;
                await bookingsTask;
            }
            else
            {
                await EnsureRealtimeSubscriptionAsync();
            }

            _hasAppeared = true;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        try
        {
            _isVisible = false;

            await _realtimeLock.WaitAsync();
            try
            {
                await _realtimeService.UnsubscribeAsync(this);
            }
            finally
            {
                _realtimeLock.Release();
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // Two feeds, because the dashboard shows two live things. The ticket follows the whole
    // business's queue while one is held — the position changes when somebody else is served, not
    // only when the customer's own row does — and falls back to the customer's own entries when
    // there is no ticket. Upcoming bookings only ever turn on the customer's own rows, so that one
    // is always customer scoped. Re-subscribing to the same feed is a no-op in the service, so this
    // is safe to call on every refresh.
    public async Task EnsureRealtimeSubscriptionAsync()
    {
        if (_customerId == Guid.Empty) return;

        await _realtimeLock.WaitAsync();
        try
        {
            // Appearing and Disappearing are both fire-and-forget, so a subscribe that was already
            // in flight can land after the page has gone. Checked inside the lock, so it sees the
            // Disappearing that is waiting on it rather than racing it.
            if (!_isVisible) return;

            if (ActiveEntry is not null)
            {
                await _realtimeService.SubscribeAsync(this, "business_id", ActiveEntry.BusinessId.ToString(),
                    () => MainThread.InvokeOnMainThreadAsync(RefreshActiveEntryAsync));
            }
            else
            {
                await _realtimeService.SubscribeAsync(this, "customer_id", _customerId.ToString(),
                    () => MainThread.InvokeOnMainThreadAsync(RefreshActiveEntryAsync));
            }

            await _realtimeService.SubscribeAsync(this, "customer_id", _customerId.ToString(),
                () => MainThread.InvokeOnMainThreadAsync(RefreshUpcomingBookingsAsync),
                table: "bookings");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            _realtimeLock.Release();
        }
    }

    public void ApplyUpcomingBookings(List<UpcomingBookingResponse> bookings)
    {
        UpcomingBookings.Clear();
        foreach (var booking in bookings.Take(UpcomingBookingsShown))
            UpcomingBookings.Add(booking);

        OnPropertyChanged(nameof(HasUpcomingBookings));
    }

    public async Task RefreshUpcomingBookingsAsync()
    {
        try
        {
            if (_customerId == Guid.Empty) return;

            ApplyUpcomingBookings(await _bookingService.GetMyUpcomingBookingsAsync(_customerId));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task RefreshActiveEntryAsync()
    {
        try
        {
            ActiveEntry = await _queueService.GetMyActiveEntryAsync();
            OnPropertyChanged(nameof(HasActiveEntry));
            await EnsureRealtimeSubscriptionAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // Everything here reads a different thing and none of it needs anything from the others, so it
    // goes out in one wave. Serially it was the sum of five round trips before the first business
    // row appeared. The business list is awaited first so a failure in one of the smaller reads
    // cannot keep the page's main content off screen.
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var isSignedIn = _customerId != Guid.Empty;

            var entryTask = _queueService.GetMyActiveEntryAsync();
            // A resolved location writes to the coordinate pair only, which drives haversine_km()
            // and the distance sort — the suburb filter itself stays on the "Lenasia" default,
            // since the app is single-suburb for now. Deriving the filter from wherever the
            // customer's GPS resolves to is the "browse elsewhere" feature (remote join for a
            // different suburb), which is a bigger, deliberately separate piece of work.
            var businessesTask = _businessService.GetBrowseBusinessesAsync(
                SelectedCategory?.Key, customerLatitude: _customerLatitude, customerLongitude: _customerLongitude);
            var bookingsTask = isSignedIn ? _bookingService.GetMyUpcomingBookingsAsync(_customerId) : null;
            var visitsTask = isSignedIn ? _queueService.GetMyVisitsAsync(_customerId) : null;

            _allBusinesses = await businessesTask;
            ApplyBusinessFilter();

            ActiveEntry = await entryTask;
            OnPropertyChanged(nameof(HasActiveEntry));

            if (bookingsTask is not null)
                ApplyUpcomingBookings(await bookingsTask);

            if (visitsTask is not null)
            {
                FrequentBusinesses.Clear();
                foreach (var item in BuildFrequentBusinesses(await visitsTask))
                    FrequentBusinesses.Add(item);
                OnPropertyChanged(nameof(HasFrequentBusinesses));
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsBusinessesEmpty));

            // Not awaited: joining the channel is a websocket handshake, and it was the first thing
            // the load waited on — RefreshActiveEntryAsync opened it before a single row was asked
            // for.
            _ = EnsureRealtimeSubscriptionAsync();
        }
    }

    public static IEnumerable<FrequentBusinessItem> BuildFrequentBusinesses(List<VisitResponse> visits) =>
        visits
            .Where(v => v.BusinessId != Guid.Empty)
            .GroupBy(v => v.BusinessId)
            .Select(g =>
            {
                var latest = g.OrderByDescending(v => v.VisitedAt).First();
                return new FrequentBusinessItem
                {
                    BusinessId = g.Key,
                    BusinessName = latest.BusinessName,
                    VisitCount = g.Count(),
                    LastVisitedAt = latest.VisitedAt,
                    LastOperatorName = latest.OperatorName,
                    LastServiceLabel = latest.ServiceLabel,
                };
            })
            .OrderByDescending(f => f.VisitCount)
            .ThenByDescending(f => f.LastVisitedAt)
            .Take(3);

    public void ApplyBusinessFilter()
    {
        try
        {
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allBusinesses
                : _allBusinesses.Where(b => b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            var ordered = filtered
                .OrderByDescending(b => b.IsAvailableNow)
                .ThenBy(b => b.DistanceKm ?? double.MaxValue)
                .ThenBy(b => b.AvgWaitMinutes ?? decimal.MaxValue)
                .ToList();

            Businesses.Clear();
            foreach (var b in ordered)
                Businesses.Add(b);

            OnPropertyChanged(nameof(IsBusinessesEmpty));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SelectCategoryAsync(ServiceCategory category)
    {
        try
        {
            if (!category.Available)
                return;

            SelectedCategory = category == SelectedCategory ? null : category;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenBusinessAsync(object? item)
    {
        Guid? businessId = item switch
        {
            BrowseBusinessSummaryResponse b => b.Id,
            FrequentBusinessItem f => f.BusinessId,
            _ => null,
        };

        if (businessId is null || businessId == Guid.Empty)
            return;

        try
        {
            var navParams = new NavigationParameters
            {
                [NavigationKeys.BusinessId] = businessId.Value,
                [NavigationKeys.OpenedFromTabs] = true,
            };
            // Modal, so the tabbed page and every tab's feed stay standing underneath and the way
            // back is a dismissal rather than a shell rebuilt from scratch.
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.BusinessDetailPage}", navParams,
                modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task CancelBookingAsync(UpcomingBookingResponse booking)
    {
        booking.IsCancelling = true;
        try
        {
            await _bookingService.CancelBookingAsync(booking.Id);
            UpcomingBookings.Remove(booking);
            OnPropertyChanged(nameof(HasUpcomingBookings));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            booking.IsCancelling = false;
        }
    }

    [RelayCommand]
    public async Task OpenDirectionsAsync()
    {
        if (ActiveEntry is null) return;

        try
        {
            if (!ActiveEntry.BusinessLatitude.HasValue || !ActiveEntry.BusinessLongitude.HasValue)
            {
                await _popupService.ShowAlertAsync("Location not set",
                    $"{ActiveEntry.BusinessName} hasn't added a map location yet.");
                return;
            }

            var location = new Location(ActiveEntry.BusinessLatitude.Value, ActiveEntry.BusinessLongitude.Value);
            await Map.Default.OpenAsync(location, new MapLaunchOptions { Name = ActiveEntry.BusinessName });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task LeaveQueueAsync()
    {
        if (ActiveEntry is null) return;

        IsLeavingQueue = true;
        try
        {
            await _queueService.CancelEntryAsync(ActiveEntry.EntryId);
            await RefreshActiveEntryAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLeavingQueue = false;
        }
    }
}
