using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.CategoryPicker.Helpers;
using QueueApp.Framework.Base;
using QueueApp.Framework.Messages;
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

// The customer-facing home screen (Browse tab). Shows a live-queue hero when the customer is
// queued somewhere, otherwise a quiet discovery band, then a category rail, upcoming bookings,
// nearby businesses with live wait times, and the businesses they visit most.
public partial class CategoryPickerPageViewModel : BaseViewModel
{
    private readonly IMessenger _messenger;
    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly ILocationService _locationService;

    private List<BrowseBusinessSummaryResponse> _allBusinesses = new();
    private Guid _customerId;
    private double? _customerLatitude;
    private double? _customerLongitude;

    // Tracks which Postgres Changes filter we're currently subscribed under, so
    // EnsureRealtimeSubscriptionAsync only tears down/reopens the socket when the scope
    // actually changes (idle <-> queued-at-a-business), not on every refresh.
    private string? _subscribedScopeKey;

    // OnAppearingAsync and OnLoadedAsync's first LoadAsync can both race to (re)subscribe on
    // first tab creation — serialize so they can't both mutate the single realtime channel at once.
    private readonly SemaphoreSlim _realtimeLock = new(1, 1);

    public CategoryPickerPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IMessenger messenger,
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
        _messenger = messenger;
        _businessService = businessService;
        _queueService = queueService;
        _bookingService = bookingService;
        _profileService = profileService;
        _authService = authService;
        _popupService = popupService;
        _realtimeService = realtimeService;
        _locationService = locationService;

        // Fody's PropertyChanged weaver raises this for every auto-property below — react to
        // search text edits here instead of hand-rolling a partial-changed hook per property.
        PropertyChanged += OnAnyPropertyChanged;
    }

    public IReadOnlyList<ServiceCategory> Categories { get; } = CategoryCatalog.All;

    public string WelcomeMessage { get; set; } = "Welcome";
    public bool IsLoading { get; set; }
    public bool IsRefreshing { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public ServiceCategory? SelectedCategory { get; set; }
    public MyActiveQueueEntryResponse? ActiveEntry { get; set; }
    public bool IsLeavingQueue { get; set; }
    public string? QuietestNowText { get; set; }
    public string LocationLabel { get; set; } = "Lenasia";
    public bool IsResolvingLocation { get; set; }

    public ObservableCollection<BrowseBusinessSummaryResponse> Businesses { get; } = new();
    public ObservableCollection<UpcomingBookingResponse> UpcomingBookings { get; } = new();
    public ObservableCollection<FrequentBusinessItem> FrequentBusinesses { get; } = new();

    public bool HasActiveEntry => ActiveEntry is not null;
    public bool HasUpcomingBookings => UpcomingBookings.Count > 0;
    public bool HasFrequentBusinesses => FrequentBusinesses.Count > 0;
    public bool IsBusinessesEmpty => Businesses.Count == 0 && !IsLoading;

    private void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchText))
            ApplyBusinessFilter();
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            var userId = await _authService.GetUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                _customerId = Guid.Parse(userId);
                var displayName = await _profileService.GetMyDisplayNameAsync(_customerId);
                WelcomeMessage = string.IsNullOrWhiteSpace(displayName) ? "Welcome" : $"Salaam, {displayName}";
            }

            // Cached location (no permission prompt, near-instant) gives the first paint real
            // distances if we've resolved one before; RefreshLocationAsync below then gets a
            // live fix and re-sorts if it's meaningfully different.
            var cached = await _locationService.GetCachedLocationAsync();
            if (cached is not null)
            {
                _customerLatitude = cached.Latitude;
                _customerLongitude = cached.Longitude;
                LocationLabel = cached.Label;
            }

            SelectedCategory ??= Categories.FirstOrDefault(c => c.Available) ?? Categories.First();
            await LoadAsync();

            // Silent: a denied/unavailable GPS fix on every automatic app-open attempt would be
            // a nagging alert, not a helpful one — only the explicit tap below (the location bar,
            // RefreshLocationCommand) surfaces a "couldn't get your location" message.
            await RefreshLocationAsync(silent: true);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private Task RefreshLocationAsync() => RefreshLocationAsync(silent: false);

    private async Task RefreshLocationAsync(bool silent)
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

            var moved = _customerLatitude != location.Latitude || _customerLongitude != location.Longitude;
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

    // This page is a persistent TabbedPage child, not a pushed page — OnLoadedAsync only fires
    // once (first tab creation) but OnAppearing/OnDisappearing fire on every tab switch, so the
    // realtime subscription lives here, symmetric with the teardown below.
    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        try
        {
            if (_customerId == Guid.Empty)
            {
                var userId = await _authService.GetUserIdAsync();
                if (!string.IsNullOrEmpty(userId))
                    _customerId = Guid.Parse(userId);
            }

            await EnsureRealtimeSubscriptionAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        await _realtimeLock.WaitAsync();
        try
        {
            await _realtimeService.UnsubscribeAsync();
            _subscribedScopeKey = null;
        }
        finally
        {
            _realtimeLock.Release();
        }
    }

    // Re-subscribes only when the scope actually needs to change:
    //  - idle: watch the customer's own rows (customer_id) so joining a queue is picked up.
    //  - queued: watch the whole business (business_id), same scope BusinessDetailPage uses, so
    //    position/serving updates caused by OTHER customers ahead of us are picked up too —
    //    not just changes to our own row.
    private async Task EnsureRealtimeSubscriptionAsync()
    {
        if (_customerId == Guid.Empty) return;

        await _realtimeLock.WaitAsync();
        try
        {
            var desiredKey = ActiveEntry is not null
                ? $"business:{ActiveEntry.BusinessId}"
                : $"customer:{_customerId}";

            if (desiredKey == _subscribedScopeKey) return;

            await _realtimeService.UnsubscribeAsync();

            if (ActiveEntry is not null)
            {
                await _realtimeService.SubscribeAsync("business_id", ActiveEntry.BusinessId.ToString(),
                    () => MainThread.InvokeOnMainThreadAsync(RefreshActiveEntryAsync));
            }
            else
            {
                await _realtimeService.SubscribeAsync("customer_id", _customerId.ToString(),
                    () => MainThread.InvokeOnMainThreadAsync(RefreshActiveEntryAsync));
            }

            _subscribedScopeKey = desiredKey;
        }
        finally
        {
            _realtimeLock.Release();
        }
    }

    private async Task RefreshActiveEntryAsync()
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

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await RefreshActiveEntryAsync();

            if (SelectedCategory is not null)
            {
                _allBusinesses = await _businessService.GetBrowseBusinessesAsync(
                    SelectedCategory.Key, customerLatitude: _customerLatitude, customerLongitude: _customerLongitude);
                ApplyBusinessFilter();
            }

            if (_customerId != Guid.Empty)
            {
                var bookings = await _bookingService.GetMyUpcomingBookingsAsync(_customerId);
                UpcomingBookings.Clear();
                foreach (var b in bookings.Take(3))
                    UpcomingBookings.Add(b);
                OnPropertyChanged(nameof(HasUpcomingBookings));

                var visits = await _queueService.GetMyVisitsAsync(_customerId);
                FrequentBusinesses.Clear();
                foreach (var item in BuildFrequentBusinesses(visits))
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
        }
    }

    private static IEnumerable<FrequentBusinessItem> BuildFrequentBusinesses(List<VisitResponse> visits) =>
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

    private void ApplyBusinessFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allBusinesses
            : _allBusinesses.Where(b => b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // Nearest-first once we know where the customer is (the "Mr Delivery" feel) — wait time
        // is still the tiebreaker for businesses at an equal/unknown distance.
        var ordered = filtered
            .OrderByDescending(b => b.IsAvailableNow)
            .ThenBy(b => b.DistanceKm ?? double.MaxValue)
            .ThenBy(b => b.AvgWaitMinutes ?? decimal.MaxValue)
            .ToList();

        Businesses.Clear();
        foreach (var b in ordered)
            Businesses.Add(b);

        var quietest = _allBusinesses
            .Where(b => b.WaitBucket is "go" or "wait")
            .OrderBy(b => b.AvgWaitMinutes)
            .FirstOrDefault();
        QuietestNowText = quietest is not null ? $"{quietest.Name} · {quietest.AvgWaitMinutes:0} min" : null;

        OnPropertyChanged(nameof(IsBusinessesEmpty));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SelectCategoryAsync(ServiceCategory category)
    {
        try
        {
            if (!category.Available || category == SelectedCategory)
            return;
             throw new NotImplementedException("Navigation to category-specific business list not yet implemented.");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        
    }

    [RelayCommand]
    private async Task OpenBusinessAsync(object? item)
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
            _messenger.Send(new NavigateAwayFromTabsMessage(
                $"/NavigationPage/{NavigationPaths.BusinessDetailPage}", navParams, true));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task SeeAllBusinessesAsync()
    {
        if (SelectedCategory is null) return;

        try
        {
            var navParams = new NavigationParameters { [NavigationKeys.Category] = SelectedCategory.Key };
            _messenger.Send(new NavigateAwayFromTabsMessage(
                $"/NavigationPage/{NavigationPaths.BusinessListPage}", navParams, true));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task CancelBookingAsync(UpcomingBookingResponse booking)
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
    private async Task OpenDirectionsAsync()
    {
        if (ActiveEntry is null) return;

        if (!ActiveEntry.BusinessLatitude.HasValue || !ActiveEntry.BusinessLongitude.HasValue)
        {
            await _popupService.ShowAlertAsync("Location not set",
                $"{ActiveEntry.BusinessName} hasn't added a map location yet.");
            return;
        }

        try
        {
            var location = new Location(ActiveEntry.BusinessLatitude.Value, ActiveEntry.BusinessLongitude.Value);
            await Map.Default.OpenAsync(location, new MapLaunchOptions { Name = ActiveEntry.BusinessName });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task LeaveQueueAsync()
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
