using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
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

namespace QueueApp.Features.Flow.Confirmation;

// What happened after a submit, and what you can still do about it: the live queue ticket, or the
// booking the shop has yet to confirm. It owns this outright — the business landing keeps only a
// strip that taps through to here.
public partial class ConfirmationPageViewModel : BaseViewModel
{
    public bool IsShowingTicket => IsInQueue;
    public bool IsShowingBooking => ActiveBooking is not null;
    public bool HasNothing => !IsShowingTicket && !IsShowingBooking && !IsLoading;

    // The one line the top bar shows. The ticket card underneath already leads with the position,
    // so this reading it off a second bar meant "You're 1st in line" twice on the same screen.
    public string HeaderText => IsShowingTicket
        ? TicketHeadline
        : IsShowingBooking ? "Request sent" : "Nothing active";
    public BusinessResponse? Business { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool IsQueueMode => Business?.Mode == FlowStepEngine.QueueMode;
    public bool IsBookingMode => Business?.Mode == FlowStepEngine.BookingMode;

    public string BusinessName => Business?.Name ?? string.Empty;

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

    private Guid _businessId;
    private bool _isVisible;
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

            await SubscribeRealtimeAsync();
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

    public async Task RefreshAsync()
    {
        try
        {
            if (IsQueueMode)
                await RefreshMyStatusAsync();
            else
                await RefreshMyBookingsAsync();

            RaiseStateChanged();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void RaiseStateChanged()
    {
        try
        {
            OnPropertyChanged(nameof(IsShowingTicket));
            OnPropertyChanged(nameof(IsShowingBooking));
            OnPropertyChanged(nameof(HasNothing));
            OnPropertyChanged(nameof(HeaderText));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // Back from here rebuilds the tabs rather than popping. Submitting replaced the stack with this
    // page precisely so there is no committed flow behind it, and reaching it from the business
    // landing's strip is the same journey a step later — either way the way out is the tabs.
    [RelayCommand]
    public async Task DoneAsync()
    {
        try
        {
            var (ownsBusiness, mode) = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
            await NavigationService.NavigateAsync(
                MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: ownsBusiness, manageMode: mode));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

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
    // The wait, the ring and the dot strip are all LiveQueueHeroView's now — it draws them off
    // ActiveQueueEntry. All that is left here is the line the top bar shows.
    public void RefreshTicket()
    {
        try
        {
            if (MyStatus is null)
                return;

            TicketHeadline = IsBeingServed
                ? $"You're up with {MyStatus.OperatorName}"
                : $"You're {Ordinal(MyStatus.Position)} in line";

            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(IsBeingServed));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void RefreshBookingConfirmation()
    {
        try
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
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
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
