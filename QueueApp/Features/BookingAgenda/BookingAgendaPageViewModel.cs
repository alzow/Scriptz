using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using MPowerKit.Popups;
using MPowerKit.Popups.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.BookingAgenda.Sheets;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Framework.Base;
using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BookingAgenda;

// Booking mode's Manage screen. A queue is reactive — someone is in the chair and you finish them.
// An agenda is planned: the day is already written and the job is to work it, clear what's blocking
// it, and sell what's still empty.
//
// The list is chronological rather than one column per resource, because every question asked here
// — who's next, am I free at two, who's waiting on me — is a question about time. Resource is a chip
// on the row and a filter, never a column (spec §2).
public partial class BookingAgendaPageViewModel : BaseViewModel
{
    private const int DayStripLength = 14;
    private static readonly TimeSpan RequestUrgentAfter = TimeSpan.FromHours(1);

    private readonly IBusinessService _businessService;
    private readonly IBookingService _bookingService;
    private readonly IOperatorService _operatorService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IPopupService _popupService;
    private readonly IQueuePopupService _alerts;

    private Guid _businessId;
    private BusinessResponse? _business;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private BusinessHours _hours = BusinessHours.Unknown;

    private List<OperatorResponse> _operators = new();
    private List<ServiceResponse> _services = new();
    private Dictionary<Guid, string> _operatorNames = new();

    private List<AgendaBookingResponse> _dayBookings = new();
    private List<AvailabilityBlockResponse> _dayBlocks = new();
    private List<AvailabilityBlockResponse> _windowBlocks = new();

    private AgendaBookingResponse? _cardBooking;
    private IDispatcherTimer? _timer;
    private PopupPage? _openSheet;

    public BookingAgendaPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IBookingService bookingService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IQueueRealtimeService realtimeService,
        IPopupService popupService,
        IQueuePopupService alerts)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _bookingService = bookingService;
        _operatorService = operatorService;
        _serviceOfferingsService = serviceOfferingsService;
        _realtimeService = realtimeService;
        _popupService = popupService;
        _alerts = alerts;
    }

    #region Chrome

    public string BusinessName { get; set; } = "Bookings";
    public bool IsOpenNow { get; set; }
    public string OpenLabel => IsOpenNow ? "OPEN" : "CLOSED";

    #endregion

    #region Day strip

    public ObservableCollection<AgendaDateOption> DateOptions { get; } = new();
    public DateTime SelectedDate { get; private set; } = LocalTime.Now.Date;

    #endregion

    #region Day stats

    public string BookedCountText { get; set; } = "0";
    public string FreeText { get; set; } = "0m";
    public string RevenueText { get; set; } = "R0";
    public string ResourceCountText { get; set; } = "0";
    public string ResourceCountLabel { get; set; } = "Staff";
    public string RevenueLabel { get; set; } = "Today";

    #endregion

    #region Requests banner

    public ObservableCollection<BookingRequestItem> Requests { get; } = new();
    public bool HasRequests => Requests.Count > 0;
    public bool IsRequestsExpanded { get; set; }
    public bool IsRequestsUrgent { get; set; }
    public string RequestsCountText { get; set; } = "";
    public string RequestsAgeText { get; set; } = "";

    #endregion

    #region Filter

    public ObservableCollection<BayFilterOption> BayFilters { get; } = new();
    private Guid? _filterOperatorId;

    #endregion

    #region Now / next card

    // Mirrors the queue board's serving card: same position, same size, same primary-action
    // placement, different words. A business switching modes shouldn't have to relearn the screen.
    public bool HasCard { get; set; }
    public string CardKicker { get; set; } = "";
    public string CardName { get; set; } = "";
    public string CardSubtitle { get; set; } = "";
    public string CardMeta { get; set; } = "";
    public string CardTimerText { get; set; } = "";
    public string CardTimerCaption { get; set; } = "";
    public string CardActionText { get; set; } = "";
    public bool IsCardBusy { get; set; }

    #endregion

    #region Agenda

    public ObservableCollection<AgendaRow> Rows { get; } = new();
    public string AgendaHeaderText { get; set; } = "REST OF TODAY";
    public bool IsLoading { get; set; }

    public bool IsClosedDay { get; set; }
    public string ClosedDayText { get; set; } = "";

    public bool IsQuietDay { get; set; }
    public string QuietDayText { get; set; } = "";

    #endregion

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            BuildDayStrip();

            _businessId = await _businessService.GetOwnedBusinessIdAsync();
            _business = await _businessService.GetBusinessAsync(_businessId);
            BusinessName = _business?.Name ?? "Bookings";
            _labels = CategoryLabels.Resolve(_business?.Category);
            ResourceCountLabel = _labels.SectionTitle;

            await LoadStaticsAsync();
            await LoadRequestsAsync();
            await LoadDayAsync();

            StartTimer();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        if (_businessId == Guid.Empty)
            return;

        await _realtimeService.SubscribeAsync("business_id", _businessId.ToString(),
            () => MainThread.InvokeOnMainThreadAsync(RefreshAsync),
            table: "bookings");
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        StopTimer();
        await _realtimeService.UnsubscribeAsync();
    }

    #region Loading

    private void BuildDayStrip()
    {
        if (DateOptions.Count > 0)
            return;

        var today = LocalTime.Now.Date;
        for (var i = 0; i < DayStripLength; i++)
            DateOptions.Add(new AgendaDateOption(today.AddDays(i)) { IsSelected = i == 0 });

        SelectedDate = today;
    }

    // Operators, services and trading hours change on the order of never during a shift, so they're
    // read once and reused across day switches — swapping days must not rebuild the page.
    private async Task LoadStaticsAsync()
    {
        _operators = await _operatorService.GetOperatorsAsync(_businessId);
        _operatorNames = _operators.ToDictionary(o => o.Id, o => o.DisplayName);
        _services = await _serviceOfferingsService.GetActiveServicesAsync(_businessId);

        ResourceCountText = _operators.Count.ToString();

        BayFilters.Clear();
        BayFilters.Add(new BayFilterOption
        {
            OperatorId = null,
            Label = $"All {_labels.PluralNoun}",
            IsSelected = _filterOperatorId is null,
        });
        foreach (var op in _operators)
        {
            BayFilters.Add(new BayFilterOption
            {
                OperatorId = op.Id,
                Label = op.DisplayName,
                IsSelected = _filterOperatorId == op.Id,
            });
        }

        // businesses has no opening-hours columns, so trading hours are the union of the active
        // operators' weekly availability — the same aggregation the customer-facing screens use.
        var windows = new List<OperatorAvailabilityResponse>();
        foreach (var op in _operators)
            windows.AddRange(await _operatorService.GetAvailabilityAsync(op.Id));

        _hours = BusinessHours.FromAvailability(windows);
        IsOpenNow = _hours.IsOpenAt(LocalTime.Now);
    }

    private async Task LoadRequestsAsync()
    {
        var today = LocalTime.Now.Date;
        var pending = await _bookingService.GetPendingRequestsAsync(_businessId, today, DayStripLength);

        _windowBlocks = await _operatorService.GetAvailabilityBlocksAsync(
            _operatorNames.Keys.ToList(),
            new DateTimeOffset(today, LocalTime.Offset),
            new DateTimeOffset(today.AddDays(DayStripLength), LocalTime.Offset));

        Requests.Clear();
        foreach (var booking in pending.OrderBy(b => b.CreatedAt))
            Requests.Add(BookingRequestItem.From(booking, _windowBlocks, _operatorNames));

        RequestsCountText = $"{Requests.Count} waiting on you";

        if (Requests.Count > 0)
        {
            var oldest = pending.Min(b => b.CreatedAt);
            var age = DateTimeOffset.UtcNow - oldest;
            IsRequestsUrgent = age > RequestUrgentAfter;
            RequestsAgeText = $"oldest asked {DescribeAge(age)} ago";
        }
        else
        {
            IsRequestsUrgent = false;
            RequestsAgeText = "";
            IsRequestsExpanded = false;
        }

        // The dot is the only thing that makes a request on a day you aren't looking at visible.
        var daysWithRequests = pending
            .Select(b => b.LocalStart.Date)
            .ToHashSet();

        foreach (var option in DateOptions)
            option.HasRequests = daysWithRequests.Contains(option.Date);

        OnPropertyChanged(nameof(HasRequests));
    }

    private async Task LoadDayAsync()
    {
        IsLoading = true;
        try
        {
            var dayStart = new DateTimeOffset(SelectedDate, LocalTime.Offset);
            var dayEnd = dayStart.AddDays(1);

            _dayBookings = await _bookingService.GetAgendaBookingsAsync(_businessId, SelectedDate);

            _dayBlocks = await _operatorService.GetAvailabilityBlocksAsync(
                _filterOperatorId is null ? _operatorNames.Keys.ToList() : new List<Guid> { _filterOperatorId.Value },
                dayStart,
                dayEnd);

            var freeSlots = await LoadFreeSlotsAsync();

            var visible = _filterOperatorId is null
                ? _dayBookings
                : _dayBookings.Where(b => b.OperatorId == _filterOperatorId).ToList();

            var shortest = _services.Count > 0 ? _services.Min(s => s.EstMinutes) : 15;

            var request = new AgendaBuildRequest
            {
                Bookings = visible,
                Blocks = _dayBlocks,
                FreeSlots = freeSlots,
                OperatorNames = _operatorNames,
                ActiveOperatorCount = _filterOperatorId is null ? _operators.Count : 1,
                ResourcePluralNoun = _labels.PluralNoun,
                ShortestServiceMinutes = shortest,
                Now = LocalTime.ToLocal(DateTimeOffset.UtcNow),
            };

            // Gap arithmetic once per day load, off the UI thread.
            var rows = await Task.Run(() => AgendaBuilder.Build(request));

            Rows.Clear();
            foreach (var row in rows)
                Rows.Add(row);

            UpdateStats(visible, rows);
            UpdateCard(visible);
            UpdateNowLine();
            UpdateDayStates(visible, rows);
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

    // Gaps come from the slot-generation engine rather than a fresh calculation: the shortest active
    // service is the finest granularity anything could be sold at, so what the engine offers for it
    // is exactly the sellable time on that day.
    private async Task<List<SlotResponse>> LoadFreeSlotsAsync()
    {
        var shortest = _services.OrderBy(s => s.EstMinutes).FirstOrDefault();
        if (shortest is null)
            return new List<SlotResponse>();

        return _filterOperatorId is null
            ? await _bookingService.GetAvailableSlotsAnyAsync(_businessId, shortest.Id, SelectedDate)
            : await _bookingService.GetAvailableSlotsAsync(_filterOperatorId.Value, shortest.Id, SelectedDate);
    }

    private void UpdateStats(IReadOnlyList<AgendaBookingResponse> bookings, IReadOnlyList<AgendaRow> rows)
    {
        var counted = bookings.Where(b => BookingStatuses.OccupiesTheDiary(b.Status)).ToList();
        BookedCountText = counted.Count.ToString();

        // Cancelled and no-show bookings earned nothing, so they're out of the day's money — a
        // revenue line that counts them is worse than no revenue line at all.
        var cents = bookings
            .Where(b => BookingStatuses.CountsTowardsRevenue(b.Status))
            .Sum(b => b.PriceCents ?? 0);
        RevenueText = cents > 0 ? MoneyFormat.Format(cents) : "R0";

        var free = rows.Where(r => r.IsGap).Aggregate(TimeSpan.Zero, (total, r) => total + (r.End - r.Start));
        FreeText = free == TimeSpan.Zero ? "0m" : AgendaBookingResponse.FormatDuration(free);

        RevenueLabel = SelectedDate == LocalTime.Now.Date ? "Today" : SelectedDate.ToString("dddd");
        AgendaHeaderText = SelectedDate == LocalTime.Now.Date
            ? "REST OF TODAY"
            : $"REST OF {SelectedDate:dddd}".ToUpperInvariant();
    }

    private void UpdateCard(IReadOnlyList<AgendaBookingResponse> bookings)
    {
        var now = LocalTime.ToLocal(DateTimeOffset.UtcNow);

        _cardBooking = bookings.FirstOrDefault(b => b.IsInProgress)
                       ?? bookings
                           .Where(b => !b.IsFinished && !b.IsInProgress && b.LocalEnd > now)
                           .OrderBy(b => b.StartsAt)
                           .FirstOrDefault();

        HasCard = _cardBooking is not null;
        if (_cardBooking is null)
            return;

        var bay = _cardBooking.Operator?.DisplayName;
        CardName = _cardBooking.CustomerName;
        CardSubtitle = bay is null
            ? _cardBooking.ServiceName
            : $"{_cardBooking.ServiceName} · {bay}";

        if (_cardBooking.IsInProgress)
        {
            CardKicker = "IN CHAIR NOW";
            CardMeta = $"Started {_cardBooking.ElapsedFrom:HH:mm} · due {_cardBooking.LocalEnd:HH:mm}";
            CardTimerCaption = $"of ~{_cardBooking.ServiceMinutes}m";
            CardActionText = "Done";
        }
        else
        {
            CardKicker = "NEXT UP";
            CardMeta = $"{_cardBooking.LocalStart:ddd d} · {_cardBooking.TimeRangeDisplay}";
            CardTimerCaption = "not arrived";
            CardActionText = "Start early";
        }

        UpdateCardTimer();
    }

    // Both the elapsed counter and the now line run off the page's single timer — not one per row.
    private void UpdateCardTimer()
    {
        if (_cardBooking is null)
            return;

        var now = LocalTime.ToLocal(DateTimeOffset.UtcNow);

        if (_cardBooking.IsInProgress)
        {
            var elapsed = now - _cardBooking.ElapsedFrom;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            CardTimerText = elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
                : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            return;
        }

        var until = _cardBooking.LocalStart - now;
        CardTimerText = until <= TimeSpan.Zero
            ? "now"
            : $"in {AgendaBookingResponse.FormatDuration(until)}";
    }

    // A green rule across the list at the current time answers "what's next" without reading a
    // single number. It belongs to a row rather than being a row, so moving it is two booleans.
    private void UpdateNowLine()
    {
        foreach (var row in Rows)
        {
            row.ShowNowLineAbove = false;
            row.ShowNowLineBelow = false;
        }

        if (SelectedDate != LocalTime.Now.Date || Rows.Count == 0)
            return;

        var now = LocalTime.ToLocal(DateTimeOffset.UtcNow);
        var label = $"NOW {now:HH:mm}";

        var next = Rows.FirstOrDefault(r => r.Start > now);
        if (next is not null)
        {
            next.NowText = label;
            next.ShowNowLineAbove = true;
            return;
        }

        var last = Rows[^1];
        last.NowText = label;
        last.ShowNowLineBelow = true;
    }

    private void UpdateDayStates(IReadOnlyList<AgendaBookingResponse> bookings, IReadOnlyList<AgendaRow> rows)
    {
        IsClosedDay = _hours.HasData && _hours.ClosingTimeOn(SelectedDate) is null && rows.Count == 0;
        ClosedDayText = IsClosedDay
            ? $"Closed on {SelectedDate:dddd}s. Nothing can be booked, and customers browsing won't see this day."
            : "";

        var free = rows.Where(r => r.IsGap).Aggregate(TimeSpan.Zero, (total, r) => total + (r.End - r.Start));
        var counted = bookings.Count(b => BookingStatuses.OccupiesTheDiary(b.Status));

        IsQuietDay = !IsClosedDay && free >= TimeSpan.FromHours(4) && counted <= 1;
        QuietDayText = IsQuietDay
            ? $"{AgendaBookingResponse.FormatDuration(free)} free on {SelectedDate:dddd}. " +
              $"Customers browsing {_business?.Suburb ?? "nearby"} will see these as bookable slots."
            : "";
    }

    private static string DescribeAge(TimeSpan age)
    {
        if (age.TotalMinutes < 60)
            return $"{Math.Max(1, (int)age.TotalMinutes)} min";
        if (age.TotalHours < 24)
        {
            var hours = (int)age.TotalHours;
            return hours == 1 ? "1 hr" : $"{hours} hrs";
        }

        var days = (int)age.TotalDays;
        return days == 1 ? "1 day" : $"{days} days";
    }

    #endregion

    #region Timer

    private void StartTimer()
    {
        if (_timer is not null)
            return;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer is null)
            return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void StopTimer()
    {
        if (_timer is null)
            return;

        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        UpdateCardTimer();
        UpdateNowLine();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadRequestsAsync();
        await LoadDayAsync();
    }

    [RelayCommand]
    private async Task SelectDateAsync(AgendaDateOption option)
    {
        if (option.Date == SelectedDate)
            return;

        foreach (var day in DateOptions)
            day.IsSelected = day == option;

        SelectedDate = option.Date;

        // Swapping the collection, not rebuilding the page: no re-subscribe, no reload of
        // operators, services or hours.
        await LoadDayAsync();
    }

    [RelayCommand]
    private async Task SelectBayAsync(BayFilterOption option)
    {
        if (option.OperatorId == _filterOperatorId)
            return;

        foreach (var chip in BayFilters)
            chip.IsSelected = chip == option;

        _filterOperatorId = option.OperatorId;
        await LoadDayAsync();
    }

    [RelayCommand]
    private void ToggleRequests()
    {
        IsRequestsExpanded = !IsRequestsExpanded;
    }

    [RelayCommand]
    private async Task ConfirmRequestAsync(BookingRequestItem item)
    {
        if (item.IsBusy) return;
        item.IsConfirming = true;
        try
        {
            await _bookingService.ConfirmBookingAsync(item.Booking.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            item.IsConfirming = false;
        }
    }

    [RelayCommand]
    private async Task DeclineRequestAsync(BookingRequestItem item)
    {
        if (item.IsBusy) return;
        item.IsDeclining = true;
        try
        {
            await _bookingService.CancelBookingAsync(item.Booking.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            item.IsDeclining = false;
        }
    }

    [RelayCommand]
    private async Task CardActionAsync()
    {
        if (_cardBooking is null || IsCardBusy)
            return;

        var booking = _cardBooking;
        IsCardBusy = true;
        try
        {
            if (booking.IsInProgress)
                await _bookingService.CompleteBookingAsync(booking.Id);
            else
                await _bookingService.StartBookingAsync(booking.Id);

            await LoadDayAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsCardBusy = false;
        }
    }

    [RelayCommand]
    private async Task RowTappedAsync(AgendaRow row)
    {
        switch (row.Kind)
        {
            case AgendaRowKind.Booking when row.Booking is not null:
                await ShowBookingActionsAsync(row.Booking);
                break;
            case AgendaRowKind.Gap:
                await ShowAddBookingAsync(row.Start, row.End);
                break;
        }
    }

    [RelayCommand]
    private Task FillGapAsync(AgendaRow row) => ShowAddBookingAsync(row.Start, row.End);

    [RelayCommand]
    private Task AddBookingAsync()
    {
        // No window in mind — offer the first gap on the day, or the start of trading.
        var gap = Rows.FirstOrDefault(r => r.IsGap);
        return gap is not null
            ? ShowAddBookingAsync(gap.Start, gap.End)
            : ShowAddBookingAsync(
                new DateTimeOffset(SelectedDate.AddHours(9), LocalTime.Offset),
                new DateTimeOffset(SelectedDate.AddHours(17), LocalTime.Offset));
    }

    [RelayCommand]
    private Task BlockTimeAsync() => ShowBlockTimeAsync();

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.BusinessSettingsPage);
    }

    #endregion

    #region Sheets

    private async Task ShowBookingActionsAsync(AgendaBookingResponse booking)
    {
        var otherResources = _operators
            .Where(o => o.Id != booking.OperatorId)
            .ToList();

        var vm = new BookingActionsSheetViewModel(booking, otherResources)
        {
            OnStart = b => RunSheetActionAsync(() => _bookingService.StartBookingAsync(b.Id)),
            OnComplete = b => RunSheetActionAsync(() => _bookingService.CompleteBookingAsync(b.Id)),
            OnNoShow = b => RunSheetActionAsync(() => _bookingService.MarkBookingNoShowAsync(b.Id)),
            OnCancel = b => RunSheetActionAsync(() => _bookingService.CancelBookingAsync(b.Id)),
            OnMoveToResource = (b, op) => RunSheetActionAsync(() =>
                _bookingService.MoveBookingAsync(b.Id, op.Id, b.StartsAt, b.EndsAt)),
            OnMoveToAnotherTime = b => MoveToAnotherTimeAsync(b),

            // The one action that doesn't close the sheet: it's a note, not a decision.
            OnSaveProgress = async (b, note) =>
            {
                try { await _bookingService.SetBookingProgressAsync(b.Id, note); }
                catch (Exception ex) { await HandleExceptionAsync(ex); }
            },
            OnDismiss = CloseSheetAsync,
        };

        await ShowSheetAsync(new BookingActionsSheet { BindingContext = vm });
    }

    private async Task MoveToAnotherTimeAsync(AgendaBookingResponse booking)
    {
        await CloseSheetAsync();

        var service = _services.FirstOrDefault(s => s.Id == booking.ServiceId) ?? _services.FirstOrDefault();
        if (service is null)
        {
            await _alerts.ShowAlertAsync("Can't move this", "This business has no active services to reschedule against.");
            return;
        }

        var vm = new MoveBookingSheetViewModel(booking, service, _operators, SelectedDate, _bookingService, _businessId)
        {
            OnMove = (b, op, start, end) => RunSheetActionAsync(() =>
                _bookingService.MoveBookingAsync(b.Id, op, start, end)),
            OnDismiss = CloseSheetAsync,
        };

        var sheet = new MoveBookingSheet { BindingContext = vm };
        await ShowSheetAsync(sheet);
        await vm.LoadAsync();
    }

    private async Task ShowAddBookingAsync(DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        if (_services.Count == 0 || _operators.Count == 0)
        {
            await _alerts.ShowAlertAsync(
                "Nothing to book yet",
                $"Add at least one service and one {_labels.Noun.ToLowerInvariant()} in settings first.");
            return;
        }

        var vm = new AddBookingSheetViewModel(_businessId, windowStart, windowEnd, _services, _operators, _labels)
        {
            OnCreate = request => RunSheetActionAsync(() => _bookingService.CreateOperatorBookingAsync(request)),
            OnDismiss = CloseSheetAsync,
        };

        await ShowSheetAsync(new AddBookingSheet { BindingContext = vm });
    }

    private async Task ShowBlockTimeAsync()
    {
        if (_operators.Count == 0)
            return;

        var vm = new BlockTimeSheetViewModel(SelectedDate, _operators, _labels, _dayBookings)
        {
            // "Whole week" reaches past the day on screen, so the warning asks the server rather
            // than reporting only on the bookings the agenda happens to have loaded.
            LoadBookingsInRange = (from, until) => _bookingService.GetBookingsInRangeAsync(_businessId, from, until),
            OnBlock = async requests =>
            {
                await CloseSheetAsync();
                try
                {
                    foreach (var request in requests)
                        await _operatorService.CreateAvailabilityBlockAsync(request);

                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    await HandleExceptionAsync(ex);
                }
            },
            OnDismiss = CloseSheetAsync,
        };

        await ShowSheetAsync(new BlockTimeSheet { BindingContext = vm });
        await vm.RecalculateAsync();
    }

    private async Task RunSheetActionAsync(Func<Task> action)
    {
        await CloseSheetAsync();
        try
        {
            await action();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    private async Task ShowSheetAsync(PopupPage page)
    {
        _openSheet = page;
        await _popupService.ShowPopupAsync(page);
    }

    private async Task CloseSheetAsync()
    {
        var page = _openSheet;
        if (page is null)
            return;

        _openSheet = null;
        await _popupService.HidePopupAsync(page);
    }

    #endregion

    // The agenda's failures were silent before this screen existed in earnest (STEP-17-SUPABASE.md
    // §5). An operator who taps Done and sees nothing happen has no way to tell a network blip from
    // a booking_status enum that has no value to put the result in, so the reason is shown.
    protected override async Task HandleExceptionAsync(Exception exception)
    {
        await base.HandleExceptionAsync(exception);
        await _alerts.ShowAlertAsync("Something went wrong", GetFriendlyErrorMessage(exception));
    }
}
