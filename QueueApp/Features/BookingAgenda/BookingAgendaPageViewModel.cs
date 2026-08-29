using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.BookingAgenda.Models;
using QueueApp.Features.BookingAgenda.Sheets;
using QueueApp.Shared.Domain;
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

public partial class BookingAgendaPageViewModel : BaseViewModel
{
    public ObservableCollection<AgendaDateOption> DateOptions { get; } = new();
    public ObservableCollection<BookingRequestItem> Requests { get; } = new();
    public ObservableCollection<BayFilterOption> BayFilters { get; } = new();
    public ObservableCollection<AgendaRow> Rows { get; } = new();
    public bool HasRows => Rows.Count > 0;

    public string BusinessName { get; set; } = "Bookings";

    // Two loaders, because they mean different things. IsInitialLoading covers the cold open, when
    // there is no header, no stats and no rows to look at. IsLoading covers a day or bay switch,
    // where the chrome stays put and only the list underneath is being replaced.
    public bool IsInitialLoading { get; set; } = true;
    public bool IsLoading { get; set; }
    public bool IsSwitchingDay => IsLoading && !IsInitialLoading;
    public bool ShowEmptyState => !IsLoading && !IsInitialLoading && Rows.Count == 0;
    public bool IsOpenNow { get; set; }
    public string OpenLabel => IsOpenNow ? "OPEN" : "CLOSED";

    public DateTime SelectedDate { get; private set; } = LocalTime.Now.Date;

    public string BookedCountText { get; set; } = "0";
    public string FreeText { get; set; } = "0m";
    public string RevenueText { get; set; } = "R0";
    public string RevenueLabel { get; set; } = "Today";
    public string ResourceCountText { get; set; } = "0";
    public string ResourceCountLabel { get; set; } = "Team";

    public bool HasRequests => Requests.Count > 0;
    public bool IsRequestsExpanded { get; set; }
    public bool IsRequestsUrgent { get; set; }
    public string RequestsCountText { get; set; } = string.Empty;
    public string RequestsAgeText { get; set; } = string.Empty;
    public Brush RequestsStroke => IsRequestsUrgent ? AgendaPalette.PurpleStroke : AgendaPalette.PurpleDimStroke;
    public double RequestsStrokeThickness => IsRequestsUrgent ? 1.5 : 1;
    public string RequestsChevron => IsRequestsExpanded ? AgendaConstants.ChevronUp : AgendaConstants.ChevronDown;

    public string AgendaHeaderText { get; set; } = "REST OF TODAY";

    public bool IsClosedDay { get; set; }
    public string ClosedDayText { get; set; } = string.Empty;
    public bool IsQuietDay { get; set; }
    public string QuietDayText { get; set; } = string.Empty;

    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private IDispatcherTimer? _tickTimer;
    private bool _hasAppeared;
    private bool _isVisible;

    // Held as a task, not a list, so the requests banner and the agenda can both await the one
    // in-flight fetch instead of racing to issue their own.
    private Task<List<AvailabilityBlockResponse>>? _windowBlocksTask;

    private Guid _businessId;
    private Guid? _filterOperatorId;
    private BusinessResponse? _business;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private BusinessHours _hours = BusinessHours.Unknown;

    private List<OperatorResponse> _operators = new();
    private List<ServiceResponse> _services = new();
    private Dictionary<Guid, string> _operatorNames = new();
    private List<AgendaBookingResponse> _dayBookings = new();
    private List<AvailabilityBlockResponse> _dayBlocks = new();
    private List<AvailabilityBlockResponse> _windowBlocks = new();

    private readonly IBusinessService _businessService;
    private readonly IBookingService _bookingService;
    private readonly IOperatorService _operatorService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;

    public BookingAgendaPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IBookingService bookingService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _bookingService = bookingService;
        _operatorService = operatorService;
        _serviceOfferingsService = serviceOfferingsService;
        _realtimeService = realtimeService;
        _popupService = popupService;
    }

    // Two waves of requests rather than a chain of ten. Everything here needs the business id and
    // nothing else needs anything from its own wave, so the only real ordering is: id, then the
    // three lookups that take it, then everything that takes an operator id.
    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            IsInitialLoading = true;
            BuildDayStrip();

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj)
                ? (Guid)idObj
                : await _businessService.GetOwnedBusinessIdAsync();

            var businessTask = _businessService.GetBusinessAsync(_businessId);
            var operatorsTask = _operatorService.GetOperatorsAsync(_businessId);
            var servicesTask = _serviceOfferingsService.GetActiveServicesAsync(_businessId);

            await Task.WhenAll(businessTask, operatorsTask, servicesTask);

            _business = await businessTask;
            BusinessName = _business?.Name ?? "Bookings";
            _labels = CategoryLabels.Resolve(_business?.Category);
            ResourceCountLabel = _labels.SectionTitle;

            _operators = await operatorsTask;
            _services = await servicesTask;
            _operatorNames = _operators.ToDictionary(o => o.Id, o => o.DisplayName);
            ResourceCountText = _operators.Count.ToString();

            BuildBayFilters();

            // The day is what the operator is looking at, so it is not made to wait behind the
            // trading hours or the requests banner.
            await Task.WhenAll(LoadHoursAsync(), LoadRequestsAsync(), LoadDayAsync());

            // IsClosedDay reads the hours, which may have landed after the rows did.
            RefreshDayStates();

            // Appearing fires before Loaded on Android, so the first pass through
            // OnAppearingAsync had no business id to filter on and skipped the subscription.
            await SubscribeRealtimeAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsInitialLoading = false;
            RaiseLoadingStateChanged();
        }
    }

    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();

            _isVisible = true;

            await SubscribeRealtimeAsync();

            StartTicking();

            // Coming back from a page pushed over this one — settings, say. Realtime was
            // unsubscribed the whole time it was up, so nothing has been putting changes into Rows.
            if (_hasAppeared)
                await RefreshAsync();

            _hasAppeared = true;
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
            StopTicking();
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
            if (!_isVisible || _businessId == Guid.Empty)
                return;

            await _realtimeService.SubscribeAsync(this, "business_id", _businessId.ToString(),
                async () => await MainThread.InvokeOnMainThreadAsync(RefreshAsync),
                table: "bookings");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void BuildDayStrip()
    {
        try
        {
            if (DateOptions.Count > 0)
                return;

            var today = LocalTime.Now.Date;

            for (var offset = 0; offset < AgendaConstants.DayStripLength; offset++)
                DateOptions.Add(new AgendaDateOption(today.AddDays(offset)) { IsSelected = offset == 0 });

            SelectedDate = today;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildBayFilters()
    {
        try
        {
            BayFilters.Clear();
            BayFilters.Add(new BayFilterOption
            {
                OperatorId = null,
                Label = $"All {_labels.PluralNoun}",
                IsSelected = _filterOperatorId is null,
            });

            foreach (var resource in _operators)
                BayFilters.Add(new BayFilterOption
                {
                    OperatorId = resource.Id,
                    Label = resource.DisplayName,
                    IsSelected = _filterOperatorId == resource.Id,
                });
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // operator_availability is per operator, so trading hours are the union across the ones on the
    // books. One request per resource, but issued together — a five-bay shop was paying five round
    // trips in a row for a header pill.
    public async Task LoadHoursAsync()
    {
        try
        {
            if (_operators.Count == 0)
                return;

            var windows = await Task.WhenAll(
                _operators.Select(o => _operatorService.GetAvailabilityAsync(o.Id)));

            _hours = BusinessHours.FromAvailability(windows.SelectMany(w => w));
            IsOpenNow = _hours.IsOpenAt(LocalTime.Now);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // Every block across the whole day strip, fetched once and shared. The requests banner checks
    // pending bookings against it and the agenda draws the selected day's blocks out of it, so
    // switching days costs nothing extra. Both callers await the same in-flight request.
    public Task<List<AvailabilityBlockResponse>> EnsureWindowBlocksAsync(bool refresh = false)
    {
        if (!refresh && _windowBlocksTask is not null)
            return _windowBlocksTask;

        var today = LocalTime.Now.Date;

        _windowBlocksTask = _operatorNames.Count == 0
            ? Task.FromResult(new List<AvailabilityBlockResponse>())
            : _operatorService.GetAvailabilityBlocksAsync(
                _operatorNames.Keys.ToList(),
                AgendaConstants.Midnight(today),
                AgendaConstants.Midnight(today.AddDays(AgendaConstants.DayStripLength)));

        return _windowBlocksTask;
    }

    public async Task LoadRequestsAsync()
    {
        try
        {
            var today = LocalTime.Now.Date;

            var pendingTask = _bookingService.GetPendingRequestsAsync(
                _businessId, today, AgendaConstants.DayStripLength);
            var blocksTask = EnsureWindowBlocksAsync();

            await Task.WhenAll(pendingTask, blocksTask);

            var pending = await pendingTask;
            _windowBlocks = await blocksTask;

            Requests.Clear();

            foreach (var booking in pending.OrderBy(b => b.CreatedAt))
                Requests.Add(BookingRequestItem.From(booking, _windowBlocks, _operatorNames));

            RequestsCountText = $"{Requests.Count} waiting on you";

            if (Requests.Count > 0)
            {
                var age = DateTimeOffset.UtcNow - pending.Min(b => b.CreatedAt);
                IsRequestsUrgent = age > TimeSpan.FromMinutes(AgendaConstants.RequestUrgentMinutes);
                RequestsAgeText = $"oldest asked {DescribeAge(age)} ago";
            }
            else
            {
                IsRequestsUrgent = false;
                RequestsAgeText = string.Empty;
                IsRequestsExpanded = false;
            }

            var daysWithRequests = pending.Select(b => b.LocalStart.Date).ToHashSet();

            foreach (var option in DateOptions)
                option.HasRequests = daysWithRequests.Contains(option.Date);

            OnPropertyChanged(nameof(HasRequests));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task LoadDayAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            IsLoading = true;
            RaiseLoadingStateChanged();

            var dayStart = AgendaConstants.Midnight(SelectedDate);
            var dayEnd = dayStart.AddDays(1);

            var bookingsTask = _bookingService.GetAgendaBookingsAsync(_businessId, SelectedDate);
            var blocksTask = EnsureWindowBlocksAsync();
            var slotsTask = LoadFreeSlotsAsync();

            await Task.WhenAll(bookingsTask, blocksTask, slotsTask);

            _dayBookings = await bookingsTask;
            var freeSlots = await slotsTask;

            // Same overlap the query uses — a block that started yesterday and runs into this
            // morning still blocks this morning.
            _dayBlocks = (await blocksTask)
                .Where(b => b.StartsAt < dayEnd && b.EndsAt > dayStart)
                .Where(b => _filterOperatorId is null || b.OperatorId == _filterOperatorId)
                .OrderBy(b => b.StartsAt)
                .ToList();

            var visible = _filterOperatorId is null
                ? _dayBookings
                : _dayBookings.Where(b => b.OperatorId == _filterOperatorId).ToList();

            var request = new AgendaBuildRequest
            {
                Bookings = visible,
                Blocks = _dayBlocks,
                FreeSlots = freeSlots,
                OperatorNames = _operatorNames,
                ActiveOperatorCount = _filterOperatorId is null ? _operators.Count : 1,
                ResourcePluralNoun = _labels.PluralNoun,
                ShortestServiceMinutes = ShortestServiceMinutes(),
                Now = LocalTime.ToLocal(DateTimeOffset.UtcNow),
            };

            var rows = await Task.Run(() => AgendaBuilder.Build(request));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Rows.Clear();

                foreach (var row in rows)
                    Rows.Add(row);

                OnPropertyChanged(nameof(HasRows));

                UpdateStats(visible, rows);
                UpdateNowLine();
                UpdateDayStates(visible, rows);
            });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
            RaiseLoadingStateChanged();
            _loadLock.Release();
        }
    }

    public void RaiseLoadingStateChanged()
    {
        try
        {
            OnPropertyChanged(nameof(IsSwitchingDay));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(HasRows));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public List<AgendaBookingResponse> VisibleBookings() =>
        _filterOperatorId is null
            ? _dayBookings
            : _dayBookings.Where(b => b.OperatorId == _filterOperatorId).ToList();

    // The closed-day and quiet-day strips read the trading hours, which on a cold open land in the
    // same wave as the rows rather than before them.
    public void RefreshDayStates()
    {
        try
        {
            UpdateDayStates(VisibleBookings(), Rows.ToList());
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public async Task<List<SlotResponse>> LoadFreeSlotsAsync()
    {
        try
        {
            var shortest = _services.OrderBy(s => s.EstMinutes).FirstOrDefault();

            if (shortest is null)
                return [];

            return _filterOperatorId is null
                ? await _bookingService.GetAvailableSlotsAnyAsync(_businessId, shortest.Id, SelectedDate)
                : await _bookingService.GetAvailableSlotsAsync(_filterOperatorId.Value, shortest.Id, SelectedDate);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public int ShortestServiceMinutes() =>
        _services.Count > 0 ? _services.Min(s => s.EstMinutes) : AgendaConstants.FallbackServiceMinutes;

    public void UpdateStats(IReadOnlyList<AgendaBookingResponse> bookings, IReadOnlyList<AgendaRow> rows)
    {
        try
        {
            BookedCountText = bookings.Count(b => BookingStatuses.OccupiesTheDiary(b.Status)).ToString();

            var cents = bookings
                .Where(b => BookingStatuses.CountsTowardsRevenue(b.Status))
                .Sum(b => b.PriceCents ?? 0);

            RevenueText = cents > 0 ? MoneyFormat.Format(cents) : "R0";

            var free = rows
                .Where(r => r.IsGap)
                .Aggregate(TimeSpan.Zero, (total, row) => total + (row.End - row.Start));

            FreeText = free == TimeSpan.Zero ? "0m" : AgendaBookingResponse.FormatDuration(free);

            var isToday = SelectedDate == LocalTime.Now.Date;
            RevenueLabel = isToday ? "Today" : SelectedDate.ToString("dddd");
            AgendaHeaderText = isToday
                ? "REST OF TODAY"
                : $"REST OF {SelectedDate:dddd}".ToUpperInvariant();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void UpdateNowLine()
    {
        try
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
        catch (Exception)
        {
        }
    }

    public void UpdateDayStates(IReadOnlyList<AgendaBookingResponse> bookings, IReadOnlyList<AgendaRow> rows)
    {
        try
        {
            IsClosedDay = _hours.HasData && _hours.ClosingTimeOn(SelectedDate) is null && rows.Count == 0;
            ClosedDayText = IsClosedDay
                ? $"Closed on {SelectedDate:dddd}s. Nothing can be booked, and customers browsing won't see this day."
                : string.Empty;

            var free = rows
                .Where(r => r.IsGap)
                .Aggregate(TimeSpan.Zero, (total, row) => total + (row.End - row.Start));

            var counted = bookings.Count(b => BookingStatuses.OccupiesTheDiary(b.Status));

            IsQuietDay = !IsClosedDay
                && free >= TimeSpan.FromHours(AgendaConstants.QuietDayFreeHours)
                && counted <= AgendaConstants.QuietDayMaxBookings;

            QuietDayText = IsQuietDay
                ? $"{AgendaBookingResponse.FormatDuration(free)} free on {SelectedDate:dddd}. " +
                  $"Customers browsing {_business?.Suburb ?? "nearby"} will see these as bookable slots."
                : string.Empty;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void StartTicking()
    {
        try
        {
            if (_tickTimer is not null)
                return;

            _tickTimer = Application.Current?.Dispatcher.CreateTimer();

            if (_tickTimer is null)
                return;

            _tickTimer.Interval = TimeSpan.FromSeconds(AgendaConstants.TickIntervalSeconds);
            _tickTimer.Tick += OnTick;
            _tickTimer.Start();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void StopTicking()
    {
        try
        {
            if (_tickTimer is null)
                return;

            _tickTimer.Stop();
            _tickTimer.Tick -= OnTick;
            _tickTimer = null;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void OnTick(object? sender, EventArgs e)
    {
        UpdateNowLine();
    }

    [RelayCommand]
    // Blocks are re-fetched here rather than reused: a refresh is what follows blocking time out or
    // confirming a request, which is exactly when the cached window has gone stale.
    public async Task RefreshAsync()
    {
        try
        {
            _ = EnsureWindowBlocksAsync(refresh: true);
            await Task.WhenAll(LoadRequestsAsync(), LoadDayAsync());
            RefreshDayStates();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SelectDateAsync(AgendaDateOption? option)
    {
        try
        {
            if (option is null || option.Date == SelectedDate)
                return;

            foreach (var day in DateOptions)
                day.IsSelected = ReferenceEquals(day, option);

            SelectedDate = option.Date;

            await LoadDayAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SelectBayAsync(BayFilterOption? option)
    {
        try
        {
            if (option is null || option.OperatorId == _filterOperatorId)
                return;

            foreach (var chip in BayFilters)
                chip.IsSelected = ReferenceEquals(chip, option);

            _filterOperatorId = option.OperatorId;

            await LoadDayAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public void ToggleRequests()
    {
        IsRequestsExpanded = !IsRequestsExpanded;
    }

    [RelayCommand]
    public async Task ConfirmRequestAsync(BookingRequestItem? item)
    {
        if (item is null || item.IsBusy)
            return;

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
    public async Task DeclineRequestAsync(BookingRequestItem? item)
    {
        if (item is null || item.IsBusy)
            return;

        item.IsDeclining = true;
        try
        {
            var reason = await _popupService.ShowPromptAsync(
                "Decline request",
                $"Why can't {item.CustomerName}'s {item.Booking.TimeRangeDisplay} booking happen? They will see this.",
                accept: "Decline",
                cancel: "Keep it",
                placeholder: "Fully booked, closed that day…");

            if (reason is null)
                return;

            await CancelWithReasonAsync(item.Booking, reason);
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
    public async Task RowTappedAsync(AgendaRow? row)
    {
        try
        {
            if (row is null)
                return;

            switch (row.Kind)
            {
                case AgendaRowKind.Booking when row.Booking is not null:
                    await OpenBookingActionsAsync(row.Booking);
                    break;

                case AgendaRowKind.Gap:
                    await OpenAddBookingAsync(row.Start);
                    break;
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task FillGapAsync(AgendaRow? row)
    {
        try
        {
            await OpenAddBookingAsync(row?.Start);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task AddBookingAsync()
    {
        try
        {
            await OpenAddBookingAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task BlockTimeAsync()
    {
        try
        {
            if (_operators.Count == 0)
                return;

            var sheet = new BlockTimeSheet(_popupService, SelectedDate, _operators, _labels, _dayBookings)
            {
                LoadBookingsInRange = (from, until) =>
                    _bookingService.GetBookingsInRangeAsync(_businessId, from, until),
            };

            await _popupService.ShowSheetAsync(sheet);
            await sheet.RecalculateAsync();

            var result = await sheet.Completion;

            if (!result.Confirmed || result.OperatorIds is null)
                return;

            foreach (var operatorId in result.OperatorIds)
                await _operatorService.CreateAvailabilityBlockAsync(new CreateAvailabilityBlockRequest
                {
                    OperatorId = operatorId,
                    StartsAt = result.StartsAt,
                    EndsAt = result.EndsAt,
                    Reason = result.Reason,
                });

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenSettingsAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.BusinessSettingsPage);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task OpenBookingActionsAsync(AgendaBookingResponse booking)
    {
        try
        {
            var others = _operators.Where(o => o.Id != booking.OperatorId).ToList();
            var sheet = new BookingActionsSheet(_popupService, booking, others);

            await _popupService.ShowSheetAsync(sheet);
            var result = await sheet.Completion;

            switch (result.Action)
            {
                case BookingAction.Complete:
                    await _bookingService.CompleteBookingAsync(booking.Id);
                    await RefreshAsync();
                    break;

                case BookingAction.MarkNoShow:
                    await _bookingService.MarkBookingNoShowAsync(booking.Id);
                    await RefreshAsync();
                    break;

                case BookingAction.Cancel:
                    await ConfirmCancelAsync(booking);
                    break;

                case BookingAction.MoveToResource when result.OperatorId is { } operatorId:
                    await _bookingService.MoveBookingAsync(booking.Id, operatorId, booking.StartsAt, booking.EndsAt);
                    await RefreshAsync();
                    break;

                case BookingAction.MoveToAnotherTime:
                    await OpenMoveBookingAsync(booking);
                    break;

                case BookingAction.SaveProgress:
                    await _bookingService.SetBookingProgressAsync(booking.Id, result.ProgressStatus);
                    await RefreshAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task ConfirmCancelAsync(AgendaBookingResponse booking)
    {
        try
        {
            var reason = await _popupService.ShowPromptAsync(
                "Cancel booking",
                $"Why is {booking.CustomerName}'s {booking.TimeRangeDisplay} booking being cancelled? They will see this.",
                accept: "Cancel booking",
                cancel: "Keep it",
                placeholder: "Bay flooded, parts didn't arrive…");

            if (reason is null)
                return;

            await CancelWithReasonAsync(booking, reason);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task CancelWithReasonAsync(AgendaBookingResponse booking, string? reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
            await _bookingService.SetCancellationReasonAsync(
                booking.Id, BookingDetails.WithCancellationReason(booking.Details, reason));

        await _bookingService.CancelBookingAsync(booking.Id);
    }

    public async Task OpenMoveBookingAsync(AgendaBookingResponse booking)
    {
        try
        {
            var service = _services.FirstOrDefault(s => s.Id == booking.ServiceId) ?? _services.FirstOrDefault();

            if (service is null)
            {
                await _popupService.ShowAlertAsync(
                    "Can't move this",
                    "This business has no active services to reschedule against.");
                return;
            }

            var sheet = new MoveBookingSheet(
                _popupService, _bookingService, booking, service, _operators, SelectedDate, _businessId);

            await _popupService.ShowSheetAsync(sheet);
            await sheet.LoadAsync();

            var result = await sheet.Completion;

            if (!result.Confirmed)
                return;

            await _bookingService.MoveBookingAsync(booking.Id, result.OperatorId, result.StartsAt, result.EndsAt);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // The same page the customer books on, in operator mode. A sheet here would have meant a second
    // slot engine, a second set of day counts and a second thing to fix every time either changed.
    public async Task OpenAddBookingAsync(DateTimeOffset? preferredStart = null)
    {
        try
        {
            if (_services.Count == 0 || _operators.Count == 0)
            {
                await _popupService.ShowAlertAsync(
                    "Nothing to book yet",
                    $"Add at least one service and one {_labels.Noun.ToLowerInvariant()} in settings first.");
                return;
            }

            var parameters = new NavigationParameters
            {
                { NavigationKeys.BusinessId, _businessId },
                { NavigationKeys.IsOperatorFlow, true },
                { NavigationKeys.PreferredDate, SelectedDate },
            };

            if (preferredStart is { } start)
                parameters.Add(NavigationKeys.PreferredStart, start);

            // The agenda is a tab, so a plain push would bury the flow inside the tab's own stack
            // with the tab bar still on screen. Modally it gets the whole window, and comes back by
            // dismissing onto the agenda it left — still standing, because a modal does not replace it.
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.BookingFlowPage}", parameters,
                modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public static string DescribeAge(TimeSpan age)
    {
        if (age.TotalMinutes < 60)
            return $"{Math.Max(1, (int)age.TotalMinutes)} min";

        if (age.TotalHours < 24)
            return (int)age.TotalHours == 1 ? "1 hr" : $"{(int)age.TotalHours} hrs";

        var days = (int)age.TotalDays;
        return days == 1 ? "1 day" : $"{days} days";
    }

    // Called from inside every catch block on this page, so it is the one method that must never
    // throw: an exception escaping here escapes the catch that was handling the first one, and
    // nothing above catches it.
    protected override async Task HandleExceptionAsync(Exception exception)
    {
        await base.HandleExceptionAsync(exception);

        try
        {
            await _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
        }
        catch (Exception)
        {
            // No page to show it on. base.HandleExceptionAsync is the whole record of it.
        }
    }
}
