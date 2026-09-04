using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Location;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;
using Refit;

namespace QueueApp.Features.BusinessDetail;

public partial class BusinessDetailPageViewModel : BaseViewModel
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Guid _businessId;
    private bool _openedFromTabs;
    private bool _isVisible;
    private BusinessHours _hours = BusinessHours.Unknown;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private List<OperatorResponse> _allOperators = new();
    private List<ServiceResponse> _services = new();
    private List<OperatorResponse> _selectableOperators = new();
    private int _servingCount;
    private MyBookingSummaryResponse? _activeBooking;
    private string _nextFreeSlotText = "—";
    private string _slotsLeftTodayText = "—";
    public BusinessResponse? Business { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool IsQueueMode => Business?.Mode == FlowStepEngine.QueueMode;
    public bool IsBookingMode => Business?.Mode == FlowStepEngine.BookingMode;

    // The three top-level states are mutually exclusive: exactly one of these renders at a time.
    // A live ticket or a booking awaiting confirmation is a strip on the landing that taps
    // through to VisitPage, which owns the detail.
    public bool HasSomethingActive => IsInQueue || _activeBooking is not null;
    public string ActiveStripText => IsInQueue ? "You're in the queue" : "Your booking is with the shop";

    // Landing — header
    public string BusinessName => Business?.Name ?? string.Empty;
    public string AddressLine => Business?.Address ?? Business?.Suburb ?? string.Empty;
    public bool IsOpen { get; set; }
    public string OpenPillText => IsOpen ? "OPEN" : "CLOSED";
    public string OpenPillTone => IsOpen ? "Good" : "Muted";

    // "WALK-IN QUEUE · MON–SAT 8:00–18:00". The hours half only appears when operator_availability
    // actually has windows for this business.
    public string ModeLine
    {
        get
        {
            var mode = IsBookingMode ? "APPOINTMENTS ONLY" : "WALK-IN QUEUE";
            return _hours.HasData ? $"{mode} · {_hours.SummaryText}" : mode;
        }
    }

    // Landing — live card
    public string PrimaryStatValue { get; set; } = "—";
    public string PrimaryStatLabel { get; set; } = "Now serving";
    public string SecondaryStatValue { get; set; } = "—";
    public string SecondaryStatLabel { get; set; } = "In queue";
    public string TertiaryStatValue { get; set; } = "—";
    public string TertiaryStatLabel { get; set; } = "Est. wait";
    public string LiveCardTitle => IsBookingMode ? "NEXT AVAILABLE" : "LIVE QUEUE";
    public string LiveCardStatus => IsBookingMode ? "Booking" : IsOpen ? "Live" : "Closed";
    public bool ShowLiveDot => IsQueueMode && IsOpen;
    public string LiveFootnote { get; set; } = string.Empty;
    public string CtaText { get; set; } = string.Empty;
    public bool IsCtaEnabled { get; set; }

    // Landing — services, team, getting there
    public ObservableCollection<ServiceChoiceItem> ServiceRows { get; } = new();
    public bool HasServices => ServiceRows.Count > 0;
    public string ServicesCountText => ServiceRows.Count > 0 ? $"All {ServiceRows.Count}" : string.Empty;

    // The landing's service list sits inside the page ScrollView, so its CollectionView is sized to
    // exactly its content — with nothing of its own left to scroll, the drag reaches the page.
    // 50 is the row template's HeightRequest; the layout adds no item spacing.
    public double ServicesListHeight => ServiceRows.Count * 50;
    public ObservableCollection<TeamMemberItem> TeamMembers { get; } = new();
    public bool HasTeam => TeamMembers.Count > 0;
    public string TeamSectionTitle => _labels.SectionTitle;
    public string TeamCountText { get; set; } = string.Empty;
    public string DistanceText { get; set; } = string.Empty;
    public bool HasDistance => !string.IsNullOrEmpty(DistanceText);

    // Flow chrome
    public ObservableCollection<QueueSummaryRow> QueueSummary { get; } = new();

    // Queue confirmation
    public MyQueueStatusResponse? MyStatus { get; set; }
    public decimal? MyWaitMinutes { get; set; }
    public bool IsInQueue => MyStatus is not null;
    public bool IsBeingServed => MyStatus?.Status == "serving";
    public bool IsLeaving { get; set; }
    public MyActiveQueueEntryResponse? ActiveQueueEntry => MyStatus is null
        ? null
        : new MyActiveQueueEntryResponse
        {
            EntryId = MyStatus.EntryId,
            BusinessId = _businessId,
            BusinessName = BusinessName,
            BusinessLatitude = Business?.Latitude,
            BusinessLongitude = Business?.Longitude,
            OperatorId = MyStatus.OperatorId,
            OperatorName = MyStatus.OperatorName,
            Position = MyStatus.Position,
            Status = MyStatus.Status,
            JoinedAt = MyStatus.JoinedAt,
            WaitMinutes = MyWaitMinutes,
            ProgressStatus = MyStatus.ProgressStatus,
        };
    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IOperatorService _operatorService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IBookingService _bookingService;
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;
    private readonly ILocationService _locationService;
    public BusinessDetailPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IBookingService bookingService,
        IProfileService profileService,
        IAuthService authService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService,
        ILocationService locationService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _queueService = queueService;
        _operatorService = operatorService;
        _serviceOfferingsService = serviceOfferingsService;
        _bookingService = bookingService;
        _profileService = profileService;
        _authService = authService;
        _realtimeService = realtimeService;
        _popupService = popupService;
        _locationService = locationService;
    }
    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("BusinessDetailPage requires a 'businessId' parameter.");
            _openedFromTabs = parameters is not null && parameters.TryGetValue(NavigationKeys.OpenedFromTabs, out var fromTabsObj)
                && fromTabsObj is true;

            IsLoading = true;

            // Two waves rather than a chain of eight. Nothing in the first needs anything but the
            // business id, and nothing in the second needs anything from its own wave, so the load
            // costs two round trips end to end instead of the sum of all of them.
            var businessTask = _businessService.GetBusinessAsync(_businessId);
            var operatorsTask = _operatorService.GetOperatorsAsync(_businessId);
            var servicesTask = _serviceOfferingsService.GetActiveServicesAsync(_businessId);

            await Task.WhenAll(businessTask, operatorsTask, servicesTask);

            Business = await businessTask;
            if (Business is null)
                throw new InvalidOperationException("That business is no longer available.");

            Title = Business.Name;
            _labels = CategoryLabels.Resolve(Business.Category);

            _allOperators = await operatorsTask;
            _selectableOperators = FlowStepEngine.SelectableOperators(_allOperators);

            _services = await servicesTask;
            ServiceRows.Clear();
            foreach (var service in _services.OrderBy(s => s.SortOrder))
                ServiceRows.Add(ServiceChoiceItem.From(service));
            OnPropertyChanged(nameof(HasServices));
            OnPropertyChanged(nameof(ServicesCountText));
            OnPropertyChanged(nameof(ServicesListHeight));

            var hoursTask = LoadHoursAsync(_allOperators);
            var distanceTask = LoadDistanceAsync();
            var liveTask = RefreshLiveStateAsync();

            _hours = await hoursTask;
            await distanceTask;
            await liveTask;

            BuildTeam();
            RefreshLandingCard();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;

            // Deliberately not awaited: opening the websocket and joining the channel is a
            // handshake the first paint does not depend on, and awaiting it here put it on the
            // critical path — connect, retry delay and all.
            _ = SubscribeRealtimeAsync();
        }
    }

    // The two live reads for whichever mode the business is in. They touch different state, so they
    // run together rather than one behind the other.
    public async Task RefreshLiveStateAsync()
    {
        if (IsQueueMode)
        {
            var queueTask = RefreshQueueAsync();
            var statusTask = RefreshMyStatusAsync();
            await queueTask;
            await statusTask;
            return;
        }

        var slotsTask = RefreshBookingSlotStatsAsync();
        var bookingsTask = RefreshMyBookingsAsync();
        await slotsTask;
        await bookingsTask;
    }
    // Re-subscribes after a page pushed over this one is popped: Loaded runs once per page, so
    // without this the feed torn down on Disappearing never comes back.
    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();
            _isVisible = true;
            await SubscribeRealtimeAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnDisappearingAsync()
    {
        try
        {
            await base.OnDisappearingAsync();
            _isVisible = false;
            await _realtimeService.UnsubscribeAsync(this);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task SubscribeRealtimeAsync()
    {
        try
        {
            if (!_isVisible || _businessId == Guid.Empty || Business is null)
                return;

            await _realtimeService.SubscribeAsync(
                this,
                "business_id",
                _businessId.ToString(),
                OnRealtimeChangeAsync,
                table: IsBookingMode ? "bookings" : "queue_entries");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task StartFlowAsync()
    {
        try
        {
            if (Business is null || !IsCtaEnabled)
                return;

            await NavigationService.NavigateAsync(
                IsBookingMode ? NavigationPaths.BookingFlowPage : NavigationPaths.QueueFlowPage,
                new NavigationParameters
                {
                    { NavigationKeys.BusinessId, _businessId },
                    { NavigationKeys.BusinessSnapshot,
                        new BusinessSnapshot(Business, _allOperators, _services, _hours) },
                });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // The strip on the landing taps through to the visit page, which owns the detail. It opens on
    // the row itself, so whichever of the two is live decides which id is handed over.
    [RelayCommand]
    public async Task OpenVisitAsync()
    {
        try
        {
            if (MyStatus is { } status)
            {
                await NavigationService.NavigateAsync(
                    NavigationPaths.VisitPage,
                    new NavigationParameters { { NavigationKeys.EntryId, status.EntryId } });
                return;
            }

            if (_activeBooking is { } booking)
                await NavigationService.NavigateAsync(
                    NavigationPaths.VisitPage,
                    new NavigationParameters { { NavigationKeys.BookingId, booking.Id } });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // Base HandleExceptionAsync only logs — surface real failures to the customer instead, most
    // notably a pooled join/booking race ("all resources are currently busy", "that time was
    // just taken") — those are normal operational states, not faults, and deserve to be seen.
    // Called from inside every catch block on this page, so it is the one method that must never
    // throw: an exception escaping here escapes the catch that was handling the first one, and
    // nothing above catches it. DisplayAlert needs a MainPage, which there isn't one of while the
    // page is still being pushed.
    protected override async Task HandleExceptionAsync(Exception exception)
    {
        var message = GetFriendlyErrorMessage(exception);
        System.Diagnostics.Debug.WriteLine($"Error: {message}");

        try
        {
            await _popupService.ShowAlertAsync("Couldn't do that", message);
        }
        catch (Exception)
        {
            // No page to show it on. The line above is the whole record of it.
        }
    }
    public async Task OnRealtimeChangeAsync() =>
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await RefreshLiveStateAsync();

                BuildTeam();
                RefreshLandingCard();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
            }
        });
    public async Task<BusinessHours> LoadHoursAsync(IReadOnlyList<OperatorResponse> operators)
    {
        try
        {
            return await _operatorService.GetBusinessHoursAsync(operators);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            return BusinessHours.Unknown;
        }
    }

    // Presence is operators.is_available; an inactive operator isn't rendered at all.
    public void BuildTeam()
    {
        try
        {
            TeamMembers.Clear();

            foreach (var op in _allOperators.Where(o => o.IsActive).OrderBy(o => o.SortOrder))
            {
                var summary = QueueSummary.FirstOrDefault(r => r.OperatorId == op.Id);
                var subLabel = !op.IsAvailable
                    ? "off today"
                    : summary is null
                        ? "free now"
                        : (summary.WaitingCount, summary.ServingCount) switch
                        {
                            (0, 0) => "free now",
                            (var waiting, 0) => $"{waiting} waiting",
                            (0, var serving) => $"{serving} being served",
                            (var waiting, var serving) => $"{waiting} waiting · {serving} being served",
                        };

                TeamMembers.Add(new TeamMemberItem
                {
                    Initials = TextFormat.Initials(op.DisplayName),
                    Name = op.DisplayName,
                    SubLabel = subLabel,
                    ShowSubLabel = !IsBookingMode,
                    IsOnShift = op.IsAvailable,
                });
            }

            var onShift = TeamMembers.Count(m => m.IsOnShift);
            TeamCountText = onShift == 0 ? "off shift" : $"{onShift} on shift";

            OnPropertyChanged(nameof(HasTeam));
            OnPropertyChanged(nameof(TeamSectionTitle));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public async Task LoadDistanceAsync()
    {
        try
        {
            if (Business?.Latitude is not { } lat || Business.Longitude is not { } lon)
                return;

            var here = await _locationService.GetCachedLocationAsync();
            if (here is null)
                return;

            var km = GeoDistance.Kilometres(here.Latitude, here.Longitude, lat, lon);
            DistanceText = $"{GeoDistance.Describe(km)} away";
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    public async Task RefreshQueueAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            // business_queue_summary reports waiting counts only, and there is no ticket sequence to
            // read a "now serving" number off, so the live card's anchor stat is counted from the
            // active entries instead. Neither read needs the other, so they go together.
            var summaryTask = _queueService.GetQueueSummaryAsync(_businessId);
            var activeTask = _queueService.GetActiveEntriesAsync(_businessId);

            var rows = await summaryTask;
            QueueSummary.Clear();
            foreach (var row in rows)
                QueueSummary.Add(row);

            var active = await activeTask;
            _servingCount = active.Count(e => e.Status == "serving");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            _loadLock.Release();
        }
    }
    public async Task RefreshMyStatusAsync()
    {
        try
        {
            // my_queue_status is the right call here — my_active_queue_entry is the dashboard's, which
            // doesn't know the business up front.
            MyStatus = await _queueService.GetMyQueueStatusAsync(_businessId);
            MyWaitMinutes = MyStatus is not null
                ? await _queueService.GetEntryWaitMinutesAsync(MyStatus.EntryId)
                : null;

            OnPropertyChanged(nameof(ActiveQueueEntry));
            OnPropertyChanged(nameof(IsInQueue));
            OnPropertyChanged(nameof(HasSomethingActive));
            OnPropertyChanged(nameof(ActiveStripText));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // get_available_slots_any unions across the business's resources, so the landing card's two slot
    // stats are genuinely shop-wide. They are measured against the shortest service — a slot that
    // fits nothing else still fits that one — which is why the day step, where a service is actually
    // chosen, goes back to the per-operator call.
    public async Task RefreshBookingSlotStatsAsync()
    {
        try
        {
            var shortest = ServiceRows.OrderBy(s => s.Service.EstMinutes).FirstOrDefault();
            if (shortest is null)
                return;

            var today = LocalTime.Now.Date;
            var todaysSlots = await _bookingService.GetAvailableSlotsAnyAsync(_businessId, shortest.Service.Id, today);
            var remaining = todaysSlots.Where(s => s.SlotStart > DateTimeOffset.UtcNow).OrderBy(s => s.SlotStart).ToList();

            _slotsLeftTodayText = remaining.Count.ToString();

            if (remaining.Count > 0)
            {
                _nextFreeSlotText = LocalTime.ToLocal(remaining[0].SlotStart).ToString("HH:mm");
                return;
            }

            var tomorrowsSlots = await _bookingService.GetAvailableSlotsAnyAsync(
                _businessId, shortest.Service.Id, today.AddDays(1));
            var next = tomorrowsSlots.OrderBy(s => s.SlotStart).FirstOrDefault();
            _nextFreeSlotText = next is null ? "—" : LocalTime.ToLocal(next.SlotStart).ToString("HH:mm");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    public async Task RefreshMyBookingsAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                return;

            var bookings = await _bookingService.GetMyBookingsAsync(_businessId, Guid.Parse(userId));
            _activeBooking = bookings
                .Where(b => b.IsCancellable && b.EndsAt > DateTimeOffset.UtcNow)
                .OrderBy(b => b.StartsAt)
                .FirstOrDefault();

            OnPropertyChanged(nameof(HasSomethingActive));
            OnPropertyChanged(nameof(ActiveStripText));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    public void RefreshLandingCard()
    {
        try
        {
            var now = LocalTime.Now;

            if (IsBookingMode)
            {
                IsOpen = !_hours.HasData || _hours.IsOpenAt(now);
                RefreshBookingCard();
            }
            else
            {
                // No opening-hours columns exist, so "open" is the live signals that do: the owner app's
                // heartbeat and whether anyone is on shift. Weekly windows narrow it further when set.
                var onShift = TeamMembers.Any(m => m.IsOnShift);
                IsOpen = (Business?.IsAvailableNow ?? false)
                    && onShift
                    && (!_hours.HasData || _hours.IsOpenAt(now));
                RefreshQueueCard();
            }

            OnPropertyChanged(nameof(OpenPillText));
            OnPropertyChanged(nameof(LiveCardStatus));
            OnPropertyChanged(nameof(ShowLiveDot));
            OnPropertyChanged(nameof(ModeLine));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void RefreshQueueCard()
    {
        try
        {
            PrimaryStatLabel = "Now serving";
            SecondaryStatLabel = "In queue";

            if (!IsOpen)
            {
                PrimaryStatValue = "—";
                SecondaryStatValue = "—";

                var next = _hours.FindNextOpening(LocalTime.Now);
                TertiaryStatLabel = next?.Label ?? "Closed";
                TertiaryStatValue = next?.TimeText ?? "—";
                CtaText = next is not null ? $"Queue opens {next.TimeText}" : "Queue is closed";
                IsCtaEnabled = true;//TODO revert later after testing. Make operator hours matter for this. Currently, the operator hours are not being used to determine if the queue is open or closed.
                LiveFootnote = "The queue reopens when the shop does";
                return;
            }

            // The design's anchor is the ticket number on the shop wall. queue_entries has no such
            // column, so this is the closest thing that is actually true — how many people are in a
            // chair right now. Restoring the wall number needs the per-day sequence.
            var waiting = QueueSummary.Sum(r => r.WaitingCount);

            // Off-shift operators come back in the summary too, with nobody waiting and a wait of
            // zero — the most attractive number on this card and the least true. Only the ones
            // actually on shift can set the headline.
            var wait = QueueSummary.FastestWaitMinutes();

            PrimaryStatValue = _servingCount.ToString();
            SecondaryStatValue = waiting.ToString();
            TertiaryStatLabel = "Est. wait";
            TertiaryStatValue = wait is { } minutes ? $"~{minutes:0} min" : "—";

            var onShift = TeamMembers.Count(m => m.IsOnShift);
            LiveFootnote = $"{onShift} of {TeamMembers.Count} {_labels.PluralNoun} on shift";

            CtaText = "Join queue";
            IsCtaEnabled = ServiceRows.Count > 0;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void RefreshBookingCard()
    {
        try
        {
            PrimaryStatLabel = "Next free slot";
            SecondaryStatLabel = "Left today";
            TertiaryStatLabel = _labels.SectionTitle;

            PrimaryStatValue = _nextFreeSlotText;
            SecondaryStatValue = _slotsLeftTodayText;
            TertiaryStatValue = _selectableOperators.Count.ToString();

            LiveFootnote = "No walk-in queue here — slots only";

            if (IsOpen)
            {
                CtaText = "Book a slot";
                IsCtaEnabled = ServiceRows.Count > 0 && _selectableOperators.Count > 0;
            }
            else
            {
                var next = _hours.FindNextOpening(LocalTime.Now);
                TertiaryStatLabel = next?.Label ?? "Closed";
                TertiaryStatValue = next?.TimeText ?? "—";
                CtaText = "Book a slot";
                IsCtaEnabled = ServiceRows.Count > 0 && _selectableOperators.Count > 0;
            }
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    [RelayCommand]
    public async Task OpenDirectionsAsync()
    {
        if (Business is null)
            return;

        try
        {
            if (Business.Latitude is not { } lat || Business.Longitude is not { } lon)
            {
                await _popupService.ShowAlertAsync("Location not set",
                    $"{Business.Name} hasn't added a map location yet.");
                return;
            }

            await Map.Default.OpenAsync(new Location(lat, lon), new MapLaunchOptions { Name = Business.Name });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            if (_openedFromTabs)
                await MainTabbedNavigation.ReturnToTabsAsync(NavigationService, _businessService);
            else
                await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
