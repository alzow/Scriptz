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
    #region Properties and fields
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Guid _businessId;
    private bool _openedFromTabs;
    private bool _isVisible;
    private BusinessHours _hours = BusinessHours.Unknown;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private List<OperatorResponse> _allOperators = new();
    private List<OperatorResponse> _selectableOperators = new();
    private int _servingCount;
    private bool _hasActiveBooking;
    private string _nextFreeSlotText = "—";
    private string _slotsLeftTodayText = "—";
    public BusinessResponse? Business { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool IsQueueMode => Business?.Mode == FlowStepEngine.QueueMode;
    public bool IsBookingMode => Business?.Mode == FlowStepEngine.BookingMode;

    // The three top-level states are mutually exclusive: exactly one of these renders at a time.
    // A live ticket or a booking awaiting confirmation is a strip on the landing that taps
    // through to ConfirmationPage, which owns the detail.
    public bool HasSomethingActive => IsInQueue || _hasActiveBooking;
    public string ActiveStripText => IsInQueue ? "You're in the queue" : "Your booking is with the shop";

    // Landing — header
    public string BusinessName => Business?.Name ?? string.Empty;
    public string AddressLine => Business?.Address ?? Business?.Suburb ?? string.Empty;
    public bool IsOpen { get; set; }
    public string OpenPillText => IsOpen ? "OPEN" : "CLOSED";

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
    #endregion
    #region Services
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
    #endregion
    #region Constructor
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
    #endregion
    #region Lifecycle
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

            Business = await _businessService.GetBusinessAsync(_businessId);
            if (Business is null)
                throw new InvalidOperationException("That business is no longer available.");

            Title = Business.Name;
            _labels = CategoryLabels.Resolve(Business.Category);

            _allOperators = await _operatorService.GetOperatorsAsync(_businessId);
            _selectableOperators = FlowStepEngine.SelectableOperators(_allOperators);

            var services = await _serviceOfferingsService.GetActiveServicesAsync(_businessId);
            ServiceRows.Clear();
            foreach (var service in services.OrderBy(s => s.SortOrder))
                ServiceRows.Add(ServiceChoiceItem.From(service));
            OnPropertyChanged(nameof(HasServices));
            OnPropertyChanged(nameof(ServicesCountText));
            OnPropertyChanged(nameof(ServicesListHeight));

            _hours = await LoadHoursAsync(_allOperators);
            await LoadDistanceAsync();

            if (IsQueueMode)
            {
                await RefreshQueueAsync();
                await RefreshMyStatusAsync();
            }
            else
            {
                await RefreshBookingSlotStatsAsync();
                await RefreshMyBookingsAsync();
            }

            BuildTeam();

            // One subscription for the whole page, scoped to this business and torn down on
            // disappearing. The confirmation states are driven off the same feed — they do not open
            // a second one.
            await SubscribeRealtimeAsync();

            RefreshLandingCard();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
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

    #endregion
    #region Rest of functions

    [RelayCommand]
    public async Task StartFlowAsync()
    {
        try
        {
            if (Business is null || !IsCtaEnabled)
                return;

            await NavigationService.NavigateAsync(
                IsBookingMode ? NavigationPaths.BookingFlowPage : NavigationPaths.QueueFlowPage,
                new NavigationParameters { { NavigationKeys.BusinessId, _businessId } });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenConfirmationAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(
                NavigationPaths.ConfirmationPage,
                new NavigationParameters { { NavigationKeys.BusinessId, _businessId } });
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
                if (IsQueueMode)
                {
                    await RefreshQueueAsync();
                    await RefreshMyStatusAsync();
                }
                else
                {
                    await RefreshBookingSlotStatsAsync();
                    await RefreshMyBookingsAsync();
                }

                BuildTeam();
                RefreshLandingCard();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
            }
        });
    // operator_availability is per operator, so the business's trading hours are the union across
    // the ones on the books. Fetched concurrently — a shop has a handful of operators, not hundreds.
    public async Task<BusinessHours> LoadHoursAsync(IReadOnlyList<OperatorResponse> operators)
    {
        try
        {
            var active = operators.Where(o => o.IsActive).ToList();
            if (active.Count == 0)
                return BusinessHours.Unknown;

            var windows = await Task.WhenAll(active.Select(o => _operatorService.GetAvailabilityAsync(o.Id)));
            return BusinessHours.FromAvailability(windows.SelectMany(w => w));
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
                    Initials = Initials(op.DisplayName),
                    Name = op.DisplayName,
                    SubLabel = subLabel,
                    ShowSubLabel = !IsBookingMode,
                    IsOnShift = op.IsAvailable,
                    RowOpacity = op.IsAvailable ? 1 : 0.4,
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

            var km = HaversineKm(here.Latitude, here.Longitude, lat, lon);
            DistanceText = km < 1
                ? $"{km * 1000:0} m away"
                : $"{km:0.#} km away";
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
            var rows = await _queueService.GetQueueSummaryAsync(_businessId);

            QueueSummary.Clear();
            foreach (var row in rows)
                QueueSummary.Add(row);

            // business_queue_summary reports waiting counts only, and there is no ticket sequence to
            // read a "now serving" number off, so the live card's anchor stat is counted from the
            // active entries instead.
            var active = await _queueService.GetActiveEntriesAsync(_businessId);
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
            _hasActiveBooking = bookings
                .Any(b => b.IsCancellable && b.EndsAt > DateTimeOffset.UtcNow);

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
                IsCtaEnabled = true;//TODO revert later after testing
                LiveFootnote = "The queue reopens when the shop does";
                return;
            }

            // The design's anchor is the ticket number on the shop wall. queue_entries has no such
            // column, so this is the closest thing that is actually true — how many people are in a
            // chair right now. Restoring the wall number needs the per-day sequence.
            var waiting = QueueSummary.Sum(r => r.WaitingCount);
            var wait = QueueSummary.Count > 0 ? QueueSummary.Min(r => r.NewJoinWaitMinutes) : 0;

            PrimaryStatValue = _servingCount.ToString();
            SecondaryStatValue = waiting.ToString();
            TertiaryStatLabel = "Est. wait";
            TertiaryStatValue = $"~{wait:0} min";

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
                CtaText = next is not null ? $"Booking opens {next.TimeText}" : "Currently closed";
                IsCtaEnabled = true;
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
            {
                var (ownsBusiness, mode) = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
                var uri = MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: ownsBusiness, manageMode: mode);
                await NavigationService.NavigateAsync(uri);
            }
            else
            {
                await NavigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    public static string Initials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
    public static string Ordinal(int value) => value switch
    {
        11 or 12 or 13 => $"{value}th",
        _ when value % 10 == 1 => $"{value}st",
        _ when value % 10 == 2 => $"{value}nd",
        _ when value % 10 == 3 => $"{value}rd",
        _ => $"{value}th",
    };
    // Static, so there is no HandleExceptionAsync to reach. A distance of 0 reads as "no distance
    // known" to the one caller, which is the honest answer if the maths ever fails.
    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        try
        {
            const double earthRadiusKm = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                    * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
        catch (Exception)
        {
            return 0;
        }
    }
    #endregion
}
