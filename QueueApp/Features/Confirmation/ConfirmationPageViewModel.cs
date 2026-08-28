using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Confirmation;

// What happened after a submit, and what you can still do about it: the live queue ticket, or the
// booking the shop has yet to confirm. It owns this outright — the business landing keeps only a
// strip that taps through to here.
public partial class ConfirmationPageViewModel : BaseViewModel
{
    public bool IsShowingTicket => IsInQueue;
    public bool IsShowingBooking => ActiveBooking is not null;
    public bool HasNothing => !IsShowingTicket && !IsShowingBooking && !IsLoading;
    public BusinessResponse? Business { get; set; }
    public bool IsLoading { get; set; }
    public bool IsQueueMode => Business?.Mode == FlowStepEngine.QueueMode;
    public bool IsBookingMode => Business?.Mode == FlowStepEngine.BookingMode;

    // The three top-level states are mutually exclusive: exactly one of these renders at a time.
    public string BusinessName => Business?.Name ?? string.Empty;
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
    public string TicketHeadline { get; set; } = string.Empty;
    public string TicketWaitText { get; set; } = string.Empty;
    public string TicketTurnText { get; set; } = string.Empty;
    public RingDrawable TicketRing { get; set; } = new(0);
    public ObservableCollection<TicketDot> TicketDots { get; } = new();

    // Booking confirmation
    public MyBookingSummaryResponse? ActiveBooking { get; set; }
    public string BookingWhenText { get; set; } = string.Empty;
    public string BookingEndsText { get; set; } = string.Empty;
    public string BookingOperatorLabel => _labels.Noun;
    public string BookingOperatorText { get; set; } = string.Empty;
    public string BookingServiceText { get; set; } = string.Empty;
    public string BookingPriceText { get; set; } = string.Empty;
    public string BookingPendingBlurb { get; set; } = string.Empty;
    public bool IsCancellingBooking { get; set; }

    // Review step
    private readonly ITicketScheme _ticketScheme = new PositionTicketScheme();
    private Guid _businessId;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);

    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;

    public ConfirmationPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IBookingService bookingService,
        IAuthService authService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _queueService = queueService;
        _bookingService = bookingService;
        _authService = authService;
        _realtimeService = realtimeService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("ConfirmationPage requires a 'businessId' parameter.");

            IsLoading = true;

            Business = await _businessService.GetBusinessAsync(_businessId)
                ?? throw new InvalidOperationException("That business is no longer available.");

            Title = Business.Name;
            _labels = CategoryLabels.Resolve(Business.Category);

            await RefreshAsync();

            await _realtimeService.SubscribeAsync(
                "business_id",
                _businessId.ToString(),
                OnRealtimeChangeAsync,
                table: IsBookingMode ? "bookings" : "queue_entries");
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
        try
        {
            await base.OnDisappearingAsync();
            await _realtimeService.UnsubscribeAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            if (IsQueueMode)
                await RefreshMyStatusAsync();
            else
                await RefreshMyBookingsAsync();

            OnPropertyChanged(nameof(IsShowingTicket));
            OnPropertyChanged(nameof(IsShowingBooking));
            OnPropertyChanged(nameof(HasNothing));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void RaiseStateChanged()
    {
        OnPropertyChanged(nameof(IsShowingTicket));
        OnPropertyChanged(nameof(IsShowingBooking));
        OnPropertyChanged(nameof(HasNothing));
    }

    [RelayCommand]
    public async Task DoneAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    protected override Task HandleExceptionAsync(Exception exception)
    {
        return _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
    }
    public async Task OnRealtimeChangeAsync() =>
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
            }
        });
    // operator_availability is per operator, so the business's trading hours are the union across
    // the ones on the books. Fetched concurrently — a shop has a handful of operators, not hundreds.
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
            RaiseStateChanged();

            RefreshTicket();
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
    public async Task RefreshMyBookingsAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                return;

            var bookings = await _bookingService.GetMyBookingsAsync(_businessId, Guid.Parse(userId));
            ActiveBooking = bookings
                .Where(b => b.IsCancellable && b.EndsAt > DateTimeOffset.UtcNow)
                .OrderBy(b => b.StartsAt)
                .FirstOrDefault();

            RaiseStateChanged();

            RefreshBookingConfirmation();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    public void RefreshTicket()
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

        OnPropertyChanged(nameof(IsBeingServed));
    }
    public void RefreshBookingConfirmation()
    {
        if (ActiveBooking is null)
            return;

        var start = LocalTime.ToLocal(ActiveBooking.StartsAt);
        var end = LocalTime.ToLocal(ActiveBooking.EndsAt);

        BookingWhenText = start.ToString("ddd d MMM · HH:mm");
        BookingEndsText = end.ToString("HH:mm");
        BookingOperatorText = ActiveBooking.OperatorName;
        BookingServiceText = ActiveBooking.ServiceName;
        BookingPriceText = ActiveBooking.PriceText;

        BookingPendingBlurb = ActiveBooking.Status == "pending"
            ? $"{ActiveBooking.OperatorName} needs to confirm. You'll get a notification — usually within an hour during trading."
            : $"{ActiveBooking.OperatorName} has confirmed. See you then.";

        OnPropertyChanged(nameof(BookingOperatorLabel));
    }
    [RelayCommand]
    public async Task LeaveQueueAsync()
    {
        if (MyStatus is null)
            return;

        IsLeaving = true;
        try
        {
            await _queueService.CancelEntryAsync(MyStatus.EntryId);
            MyStatus = null;
            MyWaitMinutes = null;

            OnPropertyChanged(nameof(ActiveQueueEntry));
            OnPropertyChanged(nameof(IsInQueue));
            RaiseStateChanged();
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
    public async Task CancelBookingAsync()
    {
        if (ActiveBooking is null)
            return;

        IsCancellingBooking = true;
        try
        {
            await _bookingService.CancelBookingAsync(ActiveBooking.Id);
            ActiveBooking = null;

            RaiseStateChanged();

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
    public static string Ordinal(int value) => value switch
    {
        11 or 12 or 13 => $"{value}th",
        _ when value % 10 == 1 => $"{value}st",
        _ when value % 10 == 2 => $"{value}nd",
        _ when value % 10 == 3 => $"{value}rd",
        _ => $"{value}th",
    };
}
