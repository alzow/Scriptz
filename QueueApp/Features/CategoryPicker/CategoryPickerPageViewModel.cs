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
using QueueApp.Services.Popup;
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

    private List<BrowseBusinessSummaryResponse> _allBusinesses = new();
    private Guid _customerId;

    public CategoryPickerPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IMessenger messenger,
        IBusinessService businessService,
        IQueueService queueService,
        IBookingService bookingService,
        IProfileService profileService,
        IAuthService authService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _messenger = messenger;
        _businessService = businessService;
        _queueService = queueService;
        _bookingService = bookingService;
        _profileService = profileService;
        _authService = authService;
        _popupService = popupService;

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

            SelectedCategory ??= Categories.FirstOrDefault(c => c.Available) ?? Categories.First();
            await LoadAsync();
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
            ActiveEntry = await _queueService.GetMyActiveEntryAsync();
            OnPropertyChanged(nameof(HasActiveEntry));

            if (SelectedCategory is not null)
            {
                _allBusinesses = await _businessService.GetBrowseBusinessesAsync(SelectedCategory.Key);
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

        var ordered = filtered
            .OrderByDescending(b => b.IsAvailableNow)
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
        if (!category.Available || category == SelectedCategory)
            return;

        SelectedCategory = category;
        await LoadAsync();
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
            var navParams = new NavigationParameters { ["businessId"] = businessId.Value };
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
            var navParams = new NavigationParameters { ["category"] = SelectedCategory.Key };
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
            ActiveEntry = null;
            OnPropertyChanged(nameof(HasActiveEntry));
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
