using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.CategoryPicker.Helpers;
using QueueApp.Features.CategoryPicker.Models;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Profile;
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
    private List<BrowseBusinessSummaryResponse> _allBusinesses = new();
    private Guid _customerId;
    private double? _customerLatitude;
    private double? _customerLongitude;
    private readonly SemaphoreSlim _realtimeLock = new(1, 1);
    private bool _isVisible;
    private bool _hasAppeared;
    private const int UpcomingBookingsShown = 3;
    public IReadOnlyList<ServiceCategory> Categories { get; } = CategoryCatalog.All.Where(c => c.Available).ToList();
    public string? CustomerDisplayName { get; set; }
    public bool IsLoading { get; set; }
    public bool IsRefreshing { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public ServiceCategory? SelectedCategory { get; set; }
    public MyActiveQueueEntryResponse? ActiveEntry { get; set; }
    public bool IsLeavingQueue { get; set; }
    public string LocationLabel { get; set; } = "Lenasia";
    public bool IsResolvingLocation { get; set; }
    public ObservableCollection<BrowseBusinessSummaryResponse> Businesses { get; } = new();
    public ObservableCollection<UpcomingBookingResponse> UpcomingBookings { get; } = new();
    public ObservableCollection<FrequentBusinessItem> FrequentBusinesses { get; } = new();

    public bool HasActiveEntry => ActiveEntry is not null;
    public bool HasUpcomingBookings => UpcomingBookings.Count > 0;
    public bool HasFrequentBusinesses => FrequentBusinesses.Count > 0;
    public bool IsBusinessesEmpty => Businesses.Count == 0 && !IsLoading;

    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IProfileService _profileService;
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
        IProfileService profileService,
        IAuthService authService,
        IQueuePopupService popupService,
        IQueueRealtimeService realtimeService,
        ILocationService locationService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _queueService = queueService;
        _bookingService = bookingService;
        _profileService = profileService;
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
            if (cached is not null)
            {
                _customerLatitude = cached.Latitude;
                _customerLongitude = cached.Longitude;
                LocationLabel = cached.Label;
            }

            await LoadAsync();

            // Not awaited: a live fix is a permission prompt on a cold install and up to twelve
            // seconds of GPS after that, and the dashboard is already on screen from the cached
            // location. It reloads itself only if the fix says the customer actually moved.
            _ = RefreshLocationAsync(silent: true);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public Task RefreshLocationAsync() => RefreshLocationAsync(silent: false);

    public async Task RefreshLocationAsync(bool silent)
    {
        IsResolvingLocation = true;
        try
        {
            var location = await _locationService.RefreshLocationAsync();
            if (location is null)
            {
                if (!silent)
                {
                    await _popupService.ShowAlertAsync("Location unavailable",
                        "Couldn't get your location — showing businesses in Lenasia instead.");
                }
                return;
            }

            var moved = location.HasMovedFrom(_customerLatitude, _customerLongitude);
            _customerLatitude = location.Latitude;
            _customerLongitude = location.Longitude;
            LocationLabel = location.Label;

            if (moved)
                await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsResolvingLocation = false;
        }
    }

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
            // the display name, frequently visited) does not go stale on a detour, so this refreshes
            // the two live things rather than reloading the whole screen.
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
            var businessesTask = _businessService.GetBrowseBusinessesAsync(
                SelectedCategory?.Key, customerLatitude: _customerLatitude, customerLongitude: _customerLongitude);
            var nameTask = isSignedIn ? _profileService.GetMyDisplayNameAsync(_customerId) : null;
            var bookingsTask = isSignedIn ? _bookingService.GetMyUpcomingBookingsAsync(_customerId) : null;
            var visitsTask = isSignedIn ? _queueService.GetMyVisitsAsync(_customerId) : null;

            _allBusinesses = await businessesTask;
            ApplyBusinessFilter();

            ActiveEntry = await entryTask;
            OnPropertyChanged(nameof(HasActiveEntry));

            if (nameTask is not null)
            {
                var displayName = await nameTask;
                CustomerDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
            }

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
    public async Task SeeAllBusinessesAsync()
    {
        try
        {
            var navParams = new NavigationParameters();
            if (SelectedCategory is not null)
                navParams[NavigationKeys.Category] = SelectedCategory.Key;

            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.BusinessListPage}", navParams,
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
