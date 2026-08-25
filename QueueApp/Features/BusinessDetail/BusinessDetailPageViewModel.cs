using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Features.BusinessDetail.Models;
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
    // No column stores a customer's travel time, so it lives per-device rather than becoming a maps
    // call on every review step. Nothing writes this yet — Profile needs a field for it.
    public const string TravelMinutesStorageKey = "customer_travel_minutes";

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

    private readonly ITicketScheme _ticketScheme = new PositionTicketScheme();
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private readonly Dictionary<(Guid OperatorId, Guid ServiceId), Dictionary<DateTime, int>> _dayCountCache = new();
    private readonly Dictionary<DateTime, List<SlotResponse>> _slotCache = new();

    private Guid _businessId;
    private bool _openedFromTabs;
    private int? _travelMinutes;
    private BusinessHours _hours = BusinessHours.Unknown;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private List<OperatorResponse> _allOperators = new();
    private List<OperatorResponse> _selectableOperators = new();
    private int _servingCount;
    private string _nextFreeSlotText = "—";
    private string _slotsLeftTodayText = "—";
    private List<FlowStep> _steps = new();
    private CancellationTokenSource? _slotDebounce;

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

    // Base HandleExceptionAsync only logs — surface real failures to the customer instead, most
    // notably a pooled join/booking race ("all resources are currently busy", "that time was
    // just taken") — those are normal operational states, not faults, and deserve to be seen.
    protected override Task HandleExceptionAsync(Exception exception)
    {
        return _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
    }

    #region Mode and page state

    public BusinessResponse? Business { get; set; }
    public bool IsLoading { get; set; }

    public bool IsQueueMode => Business?.Mode == FlowStepEngine.QueueMode;
    public bool IsBookingMode => Business?.Mode == FlowStepEngine.BookingMode;

    // The three top-level states are mutually exclusive: exactly one of these renders at a time.
    public bool IsShowingLanding => !IsFlowActive && !IsShowingConfirmation;
    public bool IsShowingConfirmation => IsInQueue || ActiveBooking is not null;

    #endregion

    #region Landing — header

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

    #endregion

    #region Landing — live card

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

    // The card's CTA scrolls away; the sticky bar takes over. Never both at once — the page toggles
    // this from the scroll position of the live card.
    public bool IsStickyCtaVisible { get; set; }
    public bool ShowStickyCta => IsStickyCtaVisible && IsShowingLanding;

    #endregion

    #region Landing — services, team, getting there

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

    #endregion

    #region Flow chrome

    public bool IsFlowActive { get; set; }
    public string FlowTitle => IsBookingMode ? "Book a slot" : "Join the queue";

    public ObservableCollection<RailSegment> RailSegments { get; } = new();
    public ObservableCollection<CrumbChip> Crumbs { get; } = new();
    public bool HasCrumbs => Crumbs.Count > 0;

    public string RailStepLabel { get; set; } = string.Empty;
    public string RailCountText { get; set; } = string.Empty;

    public string StepHeading { get; set; } = string.Empty;
    public string StepSubheading { get; set; } = string.Empty;

    public bool ShowOperatorStep { get; set; }
    public bool ShowServiceStep { get; set; }
    public bool ShowDayStep { get; set; }
    public bool ShowTimeStep { get; set; }
    public bool ShowReviewStep { get; set; }

    public string FooterLabel { get; set; } = string.Empty;
    public string FooterValue { get; set; } = string.Empty;
    public string FooterCtaText { get; set; } = "Next";
    public bool IsFooterCtaEnabled { get; set; }
    public bool IsSubmitting { get; set; }

    #endregion

    #region Flow selections

    public ObservableCollection<OperatorChoiceItem> OperatorChoices { get; } = new();
    public OperatorChoiceItem? SelectedOperatorChoice { get; set; }

    public ServiceChoiceItem? SelectedServiceRow { get; set; }

    public ObservableCollection<DayChoiceItem> DayChoices { get; } = new();
    public DayChoiceItem? SelectedDay { get; set; }
    public bool IsLoadingDays { get; set; }
    public string DayFineprint { get; set; } = string.Empty;

    public SlotPeriod Morning { get; set; } = new("MORNING", Array.Empty<SlotChoiceItem>(), "none");
    public SlotPeriod Afternoon { get; set; } = new("AFTERNOON", Array.Empty<SlotChoiceItem>(), "none");
    public SlotPeriod Evening { get; set; } = new("EVENING", Array.Empty<SlotChoiceItem>(), "none");
    public SlotChoiceItem? SelectedSlot { get; set; }
    public bool IsLoadingSlots { get; set; }

    #endregion

    #region Queue confirmation

    public MyQueueStatusResponse? MyStatus { get; set; }
    public decimal? MyWaitMinutes { get; set; }
    public bool IsInQueue => MyStatus is not null;
    public bool IsBeingServed => MyStatus?.Status == "serving";
    public bool IsLeaving { get; set; }

    public string TicketHeadline { get; set; } = string.Empty;
    public string TicketWaitText { get; set; } = string.Empty;
    public string TicketTurnText { get; set; } = string.Empty;
    public string TicketLeaveText { get; set; } = string.Empty;
    public bool ShowTicketLeaveText => !string.IsNullOrEmpty(TicketLeaveText);
    public string TicketTravelNote { get; set; } = string.Empty;
    public RingDrawable TicketRing { get; set; } = new(0);
    public ObservableCollection<TicketDot> TicketDots { get; } = new();

    #endregion

    #region Booking confirmation

    public MyBookingSummaryResponse? ActiveBooking { get; set; }
    public string BookingWhenText { get; set; } = string.Empty;
    public string BookingEndsText { get; set; } = string.Empty;
    public string BookingOperatorLabel => _labels.Noun;
    public string BookingOperatorText { get; set; } = string.Empty;
    public string BookingServiceText { get; set; } = string.Empty;
    public string BookingPriceText { get; set; } = string.Empty;
    public string BookingPendingBlurb { get; set; } = string.Empty;
    public bool IsCancellingBooking { get; set; }

    #endregion

    #region Review step

    public string ReviewOperatorLabel => _labels.Noun;
    public string ReviewOperatorText { get; set; } = string.Empty;
    public string ReviewServiceText { get; set; } = string.Empty;
    public string ReviewPriceText { get; set; } = string.Empty;
    public string ReviewPositionText { get; set; } = string.Empty;
    public string ReviewTurnText { get; set; } = string.Empty;
    public string ReviewLeaveText { get; set; } = string.Empty;
    public bool ShowReviewLeaveRow => !string.IsNullOrEmpty(ReviewLeaveText);
    public string ReviewFineprint { get; set; } = string.Empty;

    #endregion

    #region Load

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
            _travelMinutes = await ReadTravelMinutesAsync();

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
            await _realtimeService.SubscribeAsync(
                "business_id",
                _businessId.ToString(),
                OnRealtimeChangeAsync,
                table: IsBookingMode ? "bookings" : "queue_entries");

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

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        await _realtimeService.UnsubscribeAsync();
    }

    private Task OnRealtimeChangeAsync() =>
        MainThread.InvokeOnMainThreadAsync(async () =>
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
        });

    private async Task<int?> ReadTravelMinutesAsync()
    {
        var stored = await SecureStorageService.GetAsync(TravelMinutesStorageKey);
        return int.TryParse(stored, out var minutes) && minutes > 0 ? minutes : null;
    }

    // operator_availability is per operator, so the business's trading hours are the union across
    // the ones on the books. Fetched concurrently — a shop has a handful of operators, not hundreds.
    private async Task<BusinessHours> LoadHoursAsync(IReadOnlyList<OperatorResponse> operators)
    {
        var active = operators.Where(o => o.IsActive).ToList();
        if (active.Count == 0)
            return BusinessHours.Unknown;

        var windows = await Task.WhenAll(active.Select(o => _operatorService.GetAvailabilityAsync(o.Id)));
        return BusinessHours.FromAvailability(windows.SelectMany(w => w));
    }

    // Presence is operators.is_available; an inactive operator isn't rendered at all.
    private void BuildTeam()
    {
        TeamMembers.Clear();

        foreach (var op in _allOperators.Where(o => o.IsActive).OrderBy(o => o.SortOrder))
        {
            var summary = QueueSummary.FirstOrDefault(r => r.OperatorId == op.Id);
            var subLabel = !op.IsAvailable
                ? "off today"
                : summary is null || summary.WaitingCount == 0
                    ? "free now"
                    : $"{summary.WaitingCount} waiting";

            TeamMembers.Add(new TeamMemberItem
            {
                Initials = Initials(op.DisplayName),
                Name = op.DisplayName,
                SubLabel = subLabel,
                IsOnShift = op.IsAvailable,
                RowOpacity = op.IsAvailable ? 1 : 0.4,
            });
        }

        var onShift = TeamMembers.Count(m => m.IsOnShift);
        TeamCountText = onShift == 0 ? "off shift" : $"{onShift} on shift";

        OnPropertyChanged(nameof(HasTeam));
        OnPropertyChanged(nameof(TeamSectionTitle));
    }

    private async Task LoadDistanceAsync()
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

    #endregion

    #region Queue data

    public ObservableCollection<QueueSummaryRow> QueueSummary { get; } = new();

    private async Task RefreshQueueAsync()
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
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task RefreshMyStatusAsync()
    {
        // my_queue_status is the right call here — my_active_queue_entry is the dashboard's, which
        // doesn't know the business up front.
        MyStatus = await _queueService.GetMyQueueStatusAsync(_businessId);
        MyWaitMinutes = MyStatus is not null
            ? await _queueService.GetEntryWaitMinutesAsync(MyStatus.EntryId)
            : null;

        OnPropertyChanged(nameof(IsInQueue));
        OnPropertyChanged(nameof(IsShowingConfirmation));
        OnPropertyChanged(nameof(IsShowingLanding));

        RefreshTicket();
    }

    // get_available_slots_any unions across the business's resources, so the landing card's two slot
    // stats are genuinely shop-wide. They are measured against the shortest service — a slot that
    // fits nothing else still fits that one — which is why the day step, where a service is actually
    // chosen, goes back to the per-operator call.
    private async Task RefreshBookingSlotStatsAsync()
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

    private async Task RefreshMyBookingsAsync()
    {
        var userId = await _authService.GetUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            return;

        var bookings = await _bookingService.GetMyBookingsAsync(_businessId, Guid.Parse(userId));
        ActiveBooking = bookings
            .Where(b => b.IsCancellable && b.EndsAt > DateTimeOffset.UtcNow)
            .OrderBy(b => b.StartsAt)
            .FirstOrDefault();

        OnPropertyChanged(nameof(IsShowingConfirmation));
        OnPropertyChanged(nameof(IsShowingLanding));

        RefreshBookingConfirmation();
    }

    #endregion

    #region Landing card

    private void RefreshLandingCard()
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

    private void RefreshQueueCard()
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
            IsCtaEnabled = false;
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

    private void RefreshBookingCard()
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
            CtaText = next is not null ? $"Booking opens {next.TimeText}" : "Bookings are closed";
            IsCtaEnabled = false;
        }
    }

    #endregion

    #region Flow — entry and navigation

    [RelayCommand]
    private void StartFlow()
    {
        if (Business is null || !IsCtaEnabled)
            return;

        _steps = FlowStepEngine.BuildSteps(Business, _selectableOperators);

        SelectedOperatorChoice = null;
        SelectedServiceRow = null;
        SelectedDay = null;
        SelectedSlot = null;
        foreach (var row in ServiceRows)
            row.IsSelected = false;

        BuildOperatorChoices();

        // Where the operator step doesn't run, the value it would have produced still has to exist.
        if (!_steps.Contains(FlowStep.Operator))
            SelectedOperatorChoice = OperatorChoices.FirstOrDefault();

        CurrentStepIndex = 0;
        IsFlowActive = true;
        IsStickyCtaVisible = false;

        OnPropertyChanged(nameof(IsShowingLanding));
        ApplyStep();
    }

    public int CurrentStepIndex { get; set; }

    private FlowStep CurrentStep => _steps.Count > 0
        ? _steps[Math.Clamp(CurrentStepIndex, 0, _steps.Count - 1)]
        : FlowStep.Service;

    // Back on step 0 leaves the flow and clears it; anywhere else it steps back and keeps what was
    // already chosen.
    [RelayCommand]
    private void FlowBack()
    {
        if (CurrentStepIndex <= 0)
        {
            CloseFlow();
            return;
        }

        CurrentStepIndex--;
        ApplyStep();
    }

    // Android's hardware back has to mean the same thing as the on-screen one, or it pops the whole
    // page from step three. Returns true when it consumed the press.
    public bool TryHandleHardwareBack()
    {
        if (!IsFlowActive)
            return false;

        FlowBack();
        return true;
    }

    private void CloseFlow()
    {
        IsFlowActive = false;
        ShowOperatorStep = ShowServiceStep = ShowDayStep = ShowTimeStep = ShowReviewStep = false;
        Crumbs.Clear();
        OnPropertyChanged(nameof(HasCrumbs));
        OnPropertyChanged(nameof(IsShowingLanding));
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (!IsFooterCtaEnabled)
            return;

        if (CurrentStepIndex >= _steps.Count - 1)
        {
            await SubmitAsync();
            return;
        }

        CurrentStepIndex++;
        ApplyStep();
    }

    [RelayCommand]
    private void JumpToCrumb(CrumbChip? chip)
    {
        if (chip is null)
            return;

        var index = _steps.IndexOf(chip.Step);
        if (index < 0)
            return;

        CurrentStepIndex = index;
        ApplyStep();
    }

    private void ApplyStep()
    {
        var step = CurrentStep;

        ShowOperatorStep = step == FlowStep.Operator;
        ShowServiceStep = step == FlowStep.Service;
        ShowDayStep = step == FlowStep.Day;
        ShowTimeStep = step == FlowStep.Time;
        ShowReviewStep = step == FlowStep.Review;

        RailStepLabel = FlowStepEngine.RailLabel(step, _labels.Noun);
        RailCountText = $"{CurrentStepIndex + 1}/{_steps.Count}";

        RailSegments.Clear();
        for (var i = 0; i < _steps.Count; i++)
            RailSegments.Add(new RailSegment { IsDone = i < CurrentStepIndex, IsCurrent = i == CurrentStepIndex });

        BuildCrumbs();
        ApplyStepCopy(step);

        // The review step's footer quotes the position this computes, so it has to run first.
        if (step == FlowStep.Review)
            RefreshReview();

        RefreshFooter();

        if (step == FlowStep.Day)
            _ = LoadDayCountsAsync();
        else if (step == FlowStep.Time)
            _ = LoadSlotsAsync();
    }

    private void ApplyStepCopy(FlowStep step)
    {
        switch (step)
        {
            case FlowStep.Operator:
                StepHeading = _labels.StepHeading;
                StepSubheading = IsBookingMode
                    ? $"Availability is per {_labels.Noun.ToLowerInvariant()}, so this decides which times you'll see."
                    : $"Pick a {_labels.Noun.ToLowerInvariant()}, or take whoever's free first.";
                break;
            case FlowStep.Service:
                StepHeading = "What are you having?";
                StepSubheading = IsBookingMode
                    ? "Sets how long a slot has to be to fit you."
                    : "Sets how long the queue thinks you'll take.";
                break;
            case FlowStep.Day:
                StepHeading = "Which day?";
                StepSubheading = "Next 14 days. Greyed days are fully booked.";
                break;
            case FlowStep.Time:
                StepHeading = "Pick a time";
                StepSubheading = SelectedServiceRow is null
                    ? string.Empty
                    : $"{SelectedServiceRow.Name} runs {SelectedServiceRow.DurationText}. Times shown can fit it.";
                break;
            default:
                StepHeading = "Ready to join?";
                StepSubheading = "You can leave the queue any time.";
                break;
        }
    }

    private void BuildCrumbs()
    {
        Crumbs.Clear();

        for (var i = 0; i < CurrentStepIndex; i++)
        {
            var text = _steps[i] switch
            {
                FlowStep.Operator => SelectedOperatorChoice?.Name,
                FlowStep.Service => SelectedServiceRow?.Name,
                FlowStep.Day => SelectedDay is null ? null : $"{SelectedDay.DayOfWeekText} {SelectedDay.DayNumberText}",
                FlowStep.Time => SelectedSlot?.TimeText,
                _ => null,
            };

            if (!string.IsNullOrEmpty(text))
                Crumbs.Add(new CrumbChip { Step = _steps[i], Text = text });
        }

        OnPropertyChanged(nameof(HasCrumbs));
    }

    private void RefreshFooter()
    {
        var isLast = CurrentStepIndex >= _steps.Count - 1;

        switch (CurrentStep)
        {
            case FlowStep.Operator:
                FooterLabel = "Selected";
                FooterValue = SelectedOperatorChoice?.Name ?? "Nothing yet";
                IsFooterCtaEnabled = SelectedOperatorChoice is not null;
                break;
            case FlowStep.Service:
                FooterLabel = SelectedServiceRow is null
                    ? "Pick a service"
                    : $"{SelectedServiceRow.Name} · {SelectedServiceRow.DurationText}";
                FooterValue = SelectedServiceRow?.PriceText ?? string.Empty;
                IsFooterCtaEnabled = SelectedServiceRow is not null;
                break;
            case FlowStep.Day:
                FooterLabel = SelectedDay is null ? "Pick a day" : SelectedDay.Date.ToString("ddd d MMM");
                FooterValue = SelectedDay?.FreeText ?? string.Empty;
                IsFooterCtaEnabled = SelectedDay is not null;
                break;
            case FlowStep.Time:
                FooterLabel = BuildSlotRangeText();
                FooterValue = SelectedServiceRow?.PriceText ?? string.Empty;
                IsFooterCtaEnabled = SelectedSlot is not null;
                break;
            default:
                FooterLabel = "Joining as";
                FooterValue = ReviewPositionText;
                IsFooterCtaEnabled = SelectedServiceRow is not null;
                break;
        }

        FooterCtaText = isLast
            ? IsBookingMode ? "Request booking" : "Join queue"
            : "Next";
    }

    private string BuildSlotRangeText()
    {
        if (SelectedSlot is null || SelectedDay is null)
            return "Pick a time";

        var start = LocalTime.ToLocal(SelectedSlot.Slot.SlotStart);
        var end = LocalTime.ToLocal(SelectedSlot.Slot.SlotEnd);
        return $"{start:ddd d} · {start:HH:mm} – {end:HH:mm}";
    }

    #endregion

    #region Flow — selection and invalidation

    // Every downstream clear happens here. Nothing else sets a selection back to null.
    private void InvalidateAfter(FlowStep changed)
    {
        switch (changed)
        {
            // Availability is per operator; services are per business, so they survive.
            case FlowStep.Operator:
            case FlowStep.Service:
                SelectedDay = null;
                foreach (var day in DayChoices)
                    day.IsSelected = false;
                ClearSlots();
                break;
            case FlowStep.Day:
                ClearSlots();
                break;
        }
    }

    private void ClearSlots()
    {
        SelectedSlot = null;
        _slotCache.Clear();
        Morning = new SlotPeriod("MORNING", Array.Empty<SlotChoiceItem>(), "none");
        Afternoon = new SlotPeriod("AFTERNOON", Array.Empty<SlotChoiceItem>(), "none");
        Evening = new SlotPeriod("EVENING", Array.Empty<SlotChoiceItem>(), "none");
    }

    [RelayCommand]
    private void SelectOperator(OperatorChoiceItem? item)
    {
        if (item is null || ReferenceEquals(item, SelectedOperatorChoice))
            return;

        foreach (var choice in OperatorChoices)
            choice.IsSelected = ReferenceEquals(choice, item);

        SelectedOperatorChoice = item;
        InvalidateAfter(FlowStep.Operator);
        RefreshFooter();
    }

    [RelayCommand]
    private void SelectService(ServiceChoiceItem? item)
    {
        if (item is null || ReferenceEquals(item, SelectedServiceRow))
            return;

        foreach (var row in ServiceRows)
            row.IsSelected = ReferenceEquals(row, item);

        SelectedServiceRow = item;
        InvalidateAfter(FlowStep.Service);
        RefreshFooter();
    }

    [RelayCommand]
    private void SelectDay(DayChoiceItem? item)
    {
        if (item is null || !item.IsSelectable || ReferenceEquals(item, SelectedDay))
            return;

        foreach (var day in DayChoices)
            day.IsSelected = ReferenceEquals(day, item);

        SelectedDay = item;
        InvalidateAfter(FlowStep.Day);
        RefreshFooter();
    }

    [RelayCommand]
    private void SelectSlot(SlotChoiceItem? item)
    {
        if (item is null)
            return;

        foreach (var slot in Morning.Slots.Concat(Afternoon.Slots).Concat(Evening.Slots))
            slot.IsSelected = ReferenceEquals(slot, item);

        SelectedSlot = item;
        RefreshFooter();
    }

    private void BuildOperatorChoices()
    {
        OperatorChoices.Clear();

        // queue_entries.operator_id is nullable, so "any available" is a real first-class choice
        // here — pinned, tagged, and selected by default so the common path is one tap.
        if (IsQueueMode)
        {
            var fastest = QueueSummary.Count > 0 ? QueueSummary.Min(r => r.NewJoinWaitMinutes) : 0;
            var any = new OperatorChoiceItem
            {
                OperatorId = null,
                Name = "Any available",
                Initials = "★",
                SubLabel = $"Shortest wait · about {fastest:0} min",
                IsAnyAvailable = true,
                ShowFastestTag = true,
                IsSelected = true,
            };
            OperatorChoices.Add(any);
            SelectedOperatorChoice = any;
        }

        foreach (var op in _selectableOperators)
        {
            var summary = QueueSummary.FirstOrDefault(r => r.OperatorId == op.Id);
            var subLabel = IsBookingMode
                ? "Tap to see their times"
                : summary is null || summary.WaitingCount == 0
                    ? $"Free now · about {summary?.NewJoinWaitMinutes ?? 0:0} min"
                    : $"{summary.WaitingCount} waiting · about {summary.NewJoinWaitMinutes:0} min";

            OperatorChoices.Add(new OperatorChoiceItem
            {
                OperatorId = op.Id,
                Name = op.DisplayName,
                Initials = Initials(op.DisplayName),
                SubLabel = subLabel,
                IsAnyAvailable = false,
                ShowFastestTag = false,
            });
        }
    }

    #endregion

    #region Booking — days and slots

    private async Task LoadDayCountsAsync()
    {
        if (SelectedOperatorChoice?.OperatorId is not { } operatorId || SelectedServiceRow is null)
            return;

        if (DayChoices.Count == 0)
        {
            for (var i = 0; i < 14; i++)
            {
                var date = LocalTime.Now.Date.AddDays(i);
                DayChoices.Add(new DayChoiceItem
                {
                    Date = date,
                    DayOfWeekText = date.ToString("ddd").ToUpperInvariant(),
                    DayNumberText = date.Day.ToString(),
                });
            }
        }

        // Counts are per operator, so the chip has to say whose. Until the multi-resource union
        // lands, "7 free" at shop level is a number nothing can back.
        DayFineprint = SelectedServiceRow.Service.EstMinutes >= 120
            ? $"A {SelectedServiceRow.DurationText} job needs one unbroken block, so some days show fewer options than {SelectedOperatorChoice.Name} has slots."
            : $"Counts are {SelectedOperatorChoice.Name}'s free slots, not the whole shop's.";

        var serviceId = SelectedServiceRow.Service.Id;
        var key = (operatorId, serviceId);

        if (_dayCountCache.TryGetValue(key, out var cached))
        {
            ApplyDayCounts(cached);
            return;
        }

        IsLoadingDays = true;
        try
        {
            var dates = DayChoices.Select(d => d.Date).ToList();
            var results = await Task.WhenAll(dates.Select(async date =>
            {
                var slots = await _bookingService.GetAvailableSlotsAsync(operatorId, serviceId, date);
                return (date, count: slots.Count);
            }));

            var counts = results.ToDictionary(r => r.date, r => r.count);
            _dayCountCache[key] = counts;
            ApplyDayCounts(counts);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoadingDays = false;
        }
    }

    private void ApplyDayCounts(IReadOnlyDictionary<DateTime, int> counts)
    {
        var operatorName = SelectedOperatorChoice?.Name ?? string.Empty;

        foreach (var day in DayChoices)
        {
            var count = counts.TryGetValue(day.Date, out var value) ? value : 0;
            day.IsFull = count == 0;
            day.FreeText = count == 0 ? "full" : $"{count} free · {operatorName}";
        }
    }

    private async Task LoadSlotsAsync()
    {
        if (SelectedOperatorChoice?.OperatorId is not { } operatorId
            || SelectedServiceRow is null
            || SelectedDay is null)
            return;

        var date = SelectedDay.Date;

        if (_slotCache.TryGetValue(date, out var cached))
        {
            ApplySlots(cached);
            return;
        }

        // Debounced: flicking along the day strip shouldn't fire an RPC per chip.
        _slotDebounce?.Cancel();
        _slotDebounce = new CancellationTokenSource();
        var token = _slotDebounce.Token;

        IsLoadingSlots = true;
        try
        {
            await Task.Delay(250, token);

            var slots = await _bookingService.GetAvailableSlotsAsync(
                operatorId, SelectedServiceRow.Service.Id, date);

            if (token.IsCancellationRequested)
                return;

            _slotCache[date] = slots;
            ApplySlots(slots);
        }
        catch (TaskCanceledException)
        {
            // Superseded by a newer day selection.
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsLoadingSlots = false;
        }
    }

    private void ApplySlots(IReadOnlyList<SlotResponse> slots)
    {
        var items = slots
            .Select(s => new SlotChoiceItem
            {
                Slot = s,
                TimeText = LocalTime.ToLocal(s.SlotStart).ToString("HH:mm"),
            })
            .OrderBy(s => s.Slot.SlotStart)
            .ToList();

        Morning = new SlotPeriod("MORNING", InPeriod(items, 0, 12), EmptyNote(0, 12));
        Afternoon = new SlotPeriod("AFTERNOON", InPeriod(items, 12, 17), EmptyNote(12, 17));
        Evening = new SlotPeriod("EVENING", InPeriod(items, 17, 24), EmptyNote(17, 24));

        SelectedSlot = null;
        RefreshFooter();
    }

    private static List<SlotChoiceItem> InPeriod(IEnumerable<SlotChoiceItem> items, int fromHour, int toHour) =>
        items.Where(i =>
        {
            var hour = LocalTime.ToLocal(i.Slot.SlotStart).Hour;
            return hour >= fromHour && hour < toHour;
        }).ToList();

    // An absent period needs explaining or it reads as a bug — a three-hour job at a shop that shuts
    // at 17:00 genuinely has no evening, and saying so is the difference between the two.
    private string EmptyNote(int fromHour, int toHour)
    {
        if (SelectedDay is null)
            return "none";

        if (_hours.ClosingTimeOn(SelectedDay.Date) is { } closing && closing.TotalHours <= fromHour)
            return $"none — shop closes {BusinessHours.FormatClock(closing)}";

        return "none — nothing long enough left";
    }

    #endregion

    #region Review

    private void RefreshReview()
    {
        if (SelectedServiceRow is null)
            return;

        ReviewOperatorText = SelectedOperatorChoice?.Name ?? "Any available";
        ReviewServiceText = $"{SelectedServiceRow.Name} · {SelectedServiceRow.DurationText}";
        ReviewPriceText = SelectedServiceRow.PriceText;

        var row = SelectedOperatorChoice?.OperatorId is { } operatorId
            ? QueueSummary.FirstOrDefault(r => r.OperatorId == operatorId)
            : QueueSummary.OrderBy(r => r.NewJoinWaitMinutes).FirstOrDefault();

        var ahead = row?.WaitingCount ?? 0;
        var waitMinutes = row?.NewJoinWaitMinutes ?? 0;

        ReviewPositionText = Ordinal(ahead + 1) + " in line";
        var turnAt = LocalTime.Now.AddMinutes(waitMinutes);
        ReviewTurnText = turnAt.ToString("HH:mm");

        if (_travelMinutes is { } travel)
        {
            ReviewLeaveText = turnAt.AddMinutes(-travel).ToString("HH:mm");
            ReviewFineprint = $"Travel time is your saved {travel} min trip. Change it in Profile.";
        }
        else
        {
            // Without a travel value, leave-at would just be the ETA again — hide the row instead.
            ReviewLeaveText = string.Empty;
            ReviewFineprint = "Set your travel time in Profile to see when to leave.";
        }

        OnPropertyChanged(nameof(ShowReviewLeaveRow));
        OnPropertyChanged(nameof(ReviewOperatorLabel));
    }

    #endregion

    #region Submit

    private Task SubmitAsync() => IsBookingMode ? SubmitBookingAsync() : SubmitJoinAsync();

    private async Task SubmitJoinAsync()
    {
        if (SelectedServiceRow is null)
            return;

        IsSubmitting = true;
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            var customerName = await _profileService.GetMyDisplayNameAsync(Guid.Parse(userId));

            await _queueService.JoinQueueAsync(
                _businessId,
                SelectedOperatorChoice?.OperatorId,
                Guid.Parse(userId),
                customerName,
                SelectedServiceRow.Service.Id);

            CloseFlow();
            await RefreshQueueAsync();
            await RefreshMyStatusAsync();
            RefreshLandingCard();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task SubmitBookingAsync()
    {
        if (SelectedServiceRow is null || SelectedSlot is null)
            return;

        if (SelectedOperatorChoice?.OperatorId is not { } operatorId)
        {
            await HandleExceptionAsync(new InvalidOperationException(
                "Pick who's doing the work before booking."));
            return;
        }

        IsSubmitting = true;
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            await _bookingService.CreateBookingAsync(new CreateBookingRequest
            {
                BusinessId = _businessId,
                OperatorId = operatorId,
                ServiceId = SelectedServiceRow.Service.Id,
                CustomerId = Guid.Parse(userId),
                StartsAt = SelectedSlot.Slot.SlotStart,
            });

            CloseFlow();
            await RefreshMyBookingsAsync();
            RefreshLandingCard();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            // bookings_no_overlap caught a race — someone took this exact slot between the list
            // loading and the confirm tap.
            await HandleExceptionAsync(new InvalidOperationException(
                "That slot was just booked by someone else — please pick another time."));
            _slotCache.Clear();
            await LoadSlotsAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    #endregion

    #region Confirmation

    private void RefreshTicket()
    {
        TicketDots.Clear();

        if (MyStatus is null)
            return;

        TicketHeadline = IsBeingServed
            ? $"You're up with {MyStatus.OperatorName}"
            : $"You're {Ordinal(MyStatus.Position)} in line";

        var minutes = (double)(MyWaitMinutes ?? 0);
        TicketWaitText = IsBeingServed ? "now" : $"{minutes:0} min";

        var turnAt = LocalTime.Now.AddMinutes(minutes);
        TicketTurnText = turnAt.ToString("HH:mm");

        if (_travelMinutes is { } travel && !IsBeingServed)
        {
            TicketLeaveText = turnAt.AddMinutes(-travel).ToString("HH:mm");
            TicketTravelNote = $"{travel} min travel";
        }
        else
        {
            TicketLeaveText = string.Empty;
            TicketTravelNote = string.Empty;
        }

        // Compared in UTC so the ring never depends on how the JSON reader happened to tag the kind.
        var joinedUtc = MyStatus.JoinedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(MyStatus.JoinedAt, DateTimeKind.Utc)
            : MyStatus.JoinedAt.ToUniversalTime();
        var elapsed = Math.Max(0, (DateTime.UtcNow - joinedUtc).TotalMinutes);
        var total = elapsed + minutes;
        TicketRing = new RingDrawable(total > 0 ? elapsed / total : 0);

        foreach (var marker in _ticketScheme.BuildStrip(MyStatus.Position))
        {
            TicketDots.Add(new TicketDot
            {
                Label = marker.Label,
                IsNowServing = marker.IsNowServing,
                IsMine = marker.IsMine,
            });
        }

        OnPropertyChanged(nameof(ShowTicketLeaveText));
        OnPropertyChanged(nameof(IsBeingServed));
    }

    private void RefreshBookingConfirmation()
    {
        if (ActiveBooking is null)
            return;

        var start = LocalTime.ToLocal(ActiveBooking.StartsAt);
        var end = LocalTime.ToLocal(ActiveBooking.EndsAt);

        BookingWhenText = start.ToString("ddd d MMM · HH:mm");
        BookingEndsText = end.ToString("HH:mm");
        BookingOperatorText = ActiveBooking.OperatorName;
        BookingServiceText = ActiveBooking.ServiceName;
        BookingPriceText = ServiceRows
            .FirstOrDefault(s => s.Name == ActiveBooking.ServiceName)?.PriceText ?? string.Empty;

        BookingPendingBlurb = ActiveBooking.Status == "pending"
            ? $"{ActiveBooking.OperatorName} needs to confirm. You'll get a notification — usually within an hour during trading."
            : $"{ActiveBooking.OperatorName} has confirmed. See you then.";

        OnPropertyChanged(nameof(BookingOperatorLabel));
    }

    [RelayCommand]
    private async Task LeaveQueueAsync()
    {
        if (MyStatus is null)
            return;

        IsLeaving = true;
        try
        {
            await _queueService.CancelEntryAsync(MyStatus.EntryId);
            MyStatus = null;
            MyWaitMinutes = null;

            OnPropertyChanged(nameof(IsInQueue));
            OnPropertyChanged(nameof(IsShowingConfirmation));
            OnPropertyChanged(nameof(IsShowingLanding));

            await RefreshQueueAsync();
            RefreshLandingCard();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLeaving = false;
        }
    }

    [RelayCommand]
    private async Task CancelBookingAsync()
    {
        if (ActiveBooking is null)
            return;

        IsCancellingBooking = true;
        try
        {
            await _bookingService.CancelBookingAsync(ActiveBooking.Id);
            ActiveBooking = null;

            OnPropertyChanged(nameof(IsShowingConfirmation));
            OnPropertyChanged(nameof(IsShowingLanding));

            RefreshLandingCard();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsCancellingBooking = false;
        }
    }

    [RelayCommand]
    private async Task OpenDirectionsAsync()
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

    #endregion

    #region Navigation

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            if (IsFlowActive)
            {
                FlowBack();
                return;
            }

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

    #endregion

    #region Helpers

    private static string Initials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string Ordinal(int value) => value switch
    {
        11 or 12 or 13 => $"{value}th",
        _ when value % 10 == 1 => $"{value}st",
        _ when value % 10 == 2 => $"{value}nd",
        _ when value % 10 == 3 => $"{value}rd",
        _ => $"{value}th",
    };

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    #endregion
}
