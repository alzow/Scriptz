using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MPowerKit;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Messages;
using QueueApp.Features.History.Helpers;
using QueueApp.Features.History.Models;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;

namespace QueueApp.Features.History;

public partial class HistoryPageViewModel : BaseViewModel
{
    public ObservableCollection<HistoryGroup> Groups { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsEmpty => Groups.Count == 0 && !IsLoading;

    public HistoryFilter SelectedFilter { get; set; } = HistoryFilter.All;
    public bool IsAllSelected => SelectedFilter == HistoryFilter.All;
    public bool IsVisitsSelected => SelectedFilter == HistoryFilter.Visits;
    public bool IsBookingsSelected => SelectedFilter == HistoryFilter.Bookings;

    public string EmptyTitle => SelectedFilter switch
    {
        HistoryFilter.Visits => "You haven't been served anywhere yet.",
        HistoryFilter.Bookings => "No bookings yet.",
        _ => "No history yet.",
    };

    public string EmptyBody => SelectedFilter switch
    {
        HistoryFilter.Visits => "Join a queue and it'll show up here once you're done — along with who served you and how long you waited.",
        HistoryFilter.Bookings => "Not every business takes bookings yet — once you make one, it'll show up here.",
        _ => "Once you've joined a queue or made a booking, it shows up here.",
    };

    private List<MyQueueEntryResponse> _visits = new();
    private List<UpcomingBookingResponse> _bookings = new();
    private bool _isLoaded;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private readonly IMessenger _messenger;
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;

    public HistoryPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IMessenger messenger,
        IQueueService queueService,
        IBookingService bookingService,
        IAuthService authService)
        : base(navigationService, secureStorageService)
    {
        _messenger = messenger;
        _queueService = queueService;
        _bookingService = bookingService;
        _authService = authService;
    }

    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();

            if (!_isLoaded)
                return;

            await RefreshAsync();
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
            _isLoaded = true;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task RefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            OnPropertyChanged(nameof(IsEmpty));

            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            var customerId = Guid.Parse(userId);

            _visits = await _queueService.GetMyEntriesAsync(customerId);
            _bookings = await _bookingService.GetMyBookingHistoryAsync(customerId);
            Debug.WriteLine($"[History] fetched {_visits.Count} visits, {_bookings.Count} bookings for user {customerId}");

            ApplyFilter();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
            _refreshLock.Release();
        }
    }

    public void ApplyFilter()
    {
        try
        {
            IEnumerable<HistoryRow> rows = SelectedFilter switch
            {
                HistoryFilter.Visits => _visits.Select(HistoryRow.FromEntry),
                HistoryFilter.Bookings => _bookings.Select(HistoryRow.FromBooking),
                _ => _visits.Select(HistoryRow.FromEntry).Concat(_bookings.Select(HistoryRow.FromBooking)),
            };

            var now = DateTimeOffset.UtcNow;
            var upcoming = rows
                .Where(r => (r.StatusText is "CONFIRMED" or "PENDING" or "IN THE QUEUE" or "IN THE CHAIR" or "READY FOR COLLECTION")
                            && (r.Kind == HistoryRowKind.Visit || r.OccurredAt >= now))
                .OrderBy(r => r.OccurredAt)
                .ToList();
            var past = rows.Except(upcoming).OrderByDescending(r => r.OccurredAt).ToList();

            Groups.Clear();
            if (upcoming.Count > 0)
                Groups.Add(new HistoryGroup("UPCOMING", upcoming));
            foreach (var group in HistoryHelper.BucketByDate(past))
                Groups.Add(group);

            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public void SetFilter(string filter)
    {
        try
        {
            SelectedFilter = filter switch
            {
                "Visits" => HistoryFilter.Visits,
                "Bookings" => HistoryFilter.Bookings,
                _ => HistoryFilter.All,
            };

            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(IsVisitsSelected));
            OnPropertyChanged(nameof(IsBookingsSelected));
            OnPropertyChanged(nameof(EmptyTitle));
            OnPropertyChanged(nameof(EmptyBody));

            ApplyFilter();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenRowAsync(HistoryRow row)
    {
        try
        {
            if (row.RecordId == Guid.Empty)
                return;

            var key = row.Kind == HistoryRowKind.Booking ? NavigationKeys.BookingId : NavigationKeys.EntryId;

            var navParams = new NavigationParameters
            {
                [key] = row.RecordId,
                [NavigationKeys.OpenedFromTabs] = true,
            };

            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.VisitPage}", navParams,
                modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public void Browse()
    {
        try
        {
            _messenger.Send(new SelectTabMessage(NavigationPaths.CategoryPickerPage));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
}
