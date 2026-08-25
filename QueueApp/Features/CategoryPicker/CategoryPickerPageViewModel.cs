using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.CategoryPicker.Helpers;
using QueueApp.Features.CategoryPicker.Models;
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

public partial class CategoryPickerPageViewModel : BaseViewModel
{
    private List<BrowseBusinessSummaryResponse> _allBusinesses = new();
    private Guid _customerId;
    private double? _customerLatitude;
    private double? _customerLongitude;
    private string? _subscribedScopeKey;
    private readonly SemaphoreSlim _realtimeLock = new(1, 1);
    public IReadOnlyList<ServiceCategory> Categories { get; } = CategoryCatalog.All;
    public string? CustomerDisplayName { get; set; }
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

    private readonly IMessenger _messenger;
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
            {
                _customerId = Guid.Parse(userId);
                var displayName = await _profileService.GetMyDisplayNameAsync(_customerId);
                CustomerDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
            }

            var cached = await _locationService.GetCachedLocationAsync();
            if (cached is not null)
            {
                _customerLatitude = cached.Latitude;
                _customerLongitude = cached.Longitude;
                LocationLabel = cached.Label;
            }

            await LoadAsync();

            await RefreshLocationAsync(silent: true);
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
        try
        {
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task EnsureRealtimeSubscriptionAsync()
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            _realtimeLock.Release();
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

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await RefreshActiveEntryAsync();

            _allBusinesses = await _businessService.GetBrowseBusinessesAsync(
                SelectedCategory?.Key, customerLatitude: _customerLatitude, customerLongitude: _customerLongitude);
            ApplyBusinessFilter();

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

            var quietest = _allBusinesses
                .Where(b => b.WaitBucket is "go" or "wait")
                .OrderBy(b => b.AvgWaitMinutes)
                .FirstOrDefault();
            QuietestNowText = quietest is not null ? $"{quietest.Name} · {quietest.AvgWaitMinutes:0} min" : null;

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
            _messenger.Send(new NavigateAwayFromTabsMessage(
                $"/NavigationPage/{NavigationPaths.BusinessDetailPage}", navParams, true));
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

            _messenger.Send(new NavigateAwayFromTabsMessage(
                $"/NavigationPage/{NavigationPaths.BusinessListPage}", navParams, true));
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
