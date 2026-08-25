using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
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
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;
using Refit;

namespace QueueApp.Features.BusinessDetail;

public partial class BusinessDetailPageViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IOperatorService _operatorService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IBookingService _bookingService;
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Guid _businessId;
    private bool _openedFromTabs;

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
        IQueuePopupService popupService)
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
    }

    // Base HandleExceptionAsync only logs — surface real failures to the customer instead, most
    // notably a pooled join/booking race ("all resources are currently busy", "that time was
    // just taken") — those are normal operational states, not faults, and deserve to be seen.
    protected override Task HandleExceptionAsync(Exception exception)
    {
        return _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
    }

    // --- Queue-mode state ---
    public BusinessResponse? Business { get; set; }
    public ObservableCollection<QueueSummaryRow> QueueSummary { get; } = new();
    public MyQueueStatusResponse? MyStatus { get; set; }
    public decimal? MyWaitMinutes { get; set; }
    public bool IsInQueue => MyStatus != null;
    public bool IsBeingServed => MyStatus?.Status == "serving";
    public bool IsQueueMode => Business?.Mode == "queue";
    public bool IsBookingMode => Business?.Mode == "booking";
    public bool IsLoading { get; set; }
    public bool IsLeaving { get; set; }

    // Multi-resource businesses (e.g. a car wash with several bays) can let the system assign the
    // resource itself instead of making the customer pick one — allow_operator_choice only
    // changes what the customer sees; Manage-side columns/Serve/Done are unaffected either way.
    public bool AllowOperatorChoice => Business?.AllowOperatorChoice ?? true;
    public bool ShowOperatorList => IsQueueMode && AllowOperatorChoice && !IsInQueue;
    public bool ShowPooledJoin => IsQueueMode && !AllowOperatorChoice && !IsInQueue;

    // Pooled businesses show one combined figure instead of per-operator rows.
    public int PooledWaitingCount => QueueSummary.Sum(r => r.WaitingCount);
    public double PooledWaitMinutes => QueueSummary.Count > 0
        ? QueueSummary.Min(r => r.NewJoinWaitMinutes) // soonest-free resource sets the real wait
        : 0;

    // --- Queue-mode join (service picker) state ---
    public ObservableCollection<ServiceResponse> QueueServices { get; } = new();
    public ServiceResponse? SelectedQueueService { get; set; }
    public bool IsQueueServicesEmpty => QueueServices.Count == 0;
    public bool ShowQueueServicePicker { get; set; }
    public QueueSummaryRow? PendingJoinRow { get; set; }
    public bool IsPooledJoinPending { get; set; }
    public bool IsJoining { get; set; }

    // --- Booking-mode state ---
    public ObservableCollection<OperatorResponse> Operators { get; } = new();
    public OperatorResponse? SelectedOperator { get; set; }
    public bool ShowOperatorPicker => IsBookingMode && AllowOperatorChoice && Operators.Count > 1;
    public bool IsNoOperatorsAvailable => IsBookingMode && Operators.Count == 0;

    public ObservableCollection<ServiceResponse> Services { get; } = new();
    public ServiceResponse? SelectedService { get; set; }

    // Pooled businesses don't wait on an operator pick that's never coming.
    public bool ShowServiceSection => IsBookingMode && (!AllowOperatorChoice || SelectedOperator is not null);
    public bool IsServicesEmpty => ShowServiceSection && Services.Count == 0;

    public List<DateTime> DateOptions { get; } =
        Enumerable.Range(0, 14).Select(i => DateTime.Today.AddDays(i)).ToList();
    public DateTime? SelectedDate { get; set; }
    public bool ShowDateSection => IsBookingMode && SelectedService is not null;

    public ObservableCollection<SlotResponse> Slots { get; } = new();
    public SlotResponse? SelectedSlot { get; set; }
    public bool ShowSlotSection => IsBookingMode && SelectedDate is not null;
    public bool IsLoadingSlots { get; set; }
    public bool IsSlotsEmpty => Slots.Count == 0 && !IsLoadingSlots;

    public bool IsConfirmingBooking { get; set; }

    public ObservableCollection<MyBookingSummaryResponse> MyBookings { get; } = new();
    public bool ShowBookingHistory => IsBookingMode && MyBookings.Count > 0;

    public bool ShowConfirmationToast { get; set; }
    public string ConfirmedOperatorName { get; set; } = "";
    public string ConfirmedServiceName { get; set; } = "";
    public string ConfirmedDateDisplay { get; set; } = "";
    public string ConfirmedTimeDisplay { get; set; } = "";

    [RelayCommand]
    private async Task GoBackAsync()
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
            Title = Business?.Name ?? "";

            if (IsQueueMode)
            {
                var queueServices = await _serviceOfferingsService.GetActiveServicesAsync(_businessId);
                QueueServices.Clear();
                foreach (var service in queueServices)
                    QueueServices.Add(service);
                OnPropertyChanged(nameof(IsQueueServicesEmpty));

                await RefreshQueueAsync();
                await RefreshMyStatusAsync();
                await _realtimeService.SubscribeAsync("business_id", _businessId.ToString(),
                    async () => await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await RefreshMyStatusAsync();
                        await RefreshQueueAsync();
                    }));
            }
            else if (IsBookingMode)
            {
                var operators = await _operatorService.GetOperatorsAsync(_businessId);
                Operators.Clear();
                foreach (var op in operators)
                    Operators.Add(op);
                OnPropertyChanged(nameof(ShowOperatorPicker));
                OnPropertyChanged(nameof(IsNoOperatorsAvailable));

                var services = await _serviceOfferingsService.GetActiveServicesAsync(_businessId);
                Services.Clear();
                foreach (var service in services)
                    Services.Add(service);
                OnPropertyChanged(nameof(IsServicesEmpty));

                if (Operators.Count == 1)
                    SelectedOperator = Operators[0];

                var userId = await _authService.GetUserIdAsync();
                if (!string.IsNullOrEmpty(userId))
                {
                    await LoadMyBookingsAsync(Guid.Parse(userId));

                    await _realtimeService.SubscribeAsync("business_id", _businessId.ToString(),
                        async () => await MainThread.InvokeOnMainThreadAsync(() => LoadMyBookingsAsync(Guid.Parse(userId))),
                        table: "bookings");
                }
            }

            IsLoading = false;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();

        if (IsQueueMode || IsBookingMode)
            await _realtimeService.UnsubscribeAsync();
    }

    private async Task LoadMyBookingsAsync(Guid customerId)
    {
        try
        {
            var bookings = await _bookingService.GetMyBookingsAsync(_businessId, customerId);
            MyBookings.Clear();
            foreach (var b in bookings)
                MyBookings.Add(b);
            OnPropertyChanged(nameof(ShowBookingHistory));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task CancelBookingAsync(MyBookingSummaryResponse booking)
    {
        booking.IsCancelling = true;
        try
        {
            await _bookingService.CancelBookingAsync(booking.Id);
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

    private async Task RefreshQueueAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            var rows = await _queueService.GetQueueSummaryAsync(_businessId);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                QueueSummary.Clear();
                foreach (var row in rows)
                    QueueSummary.Add(row);

                // LINQ over QueueSummary, not a direct property read — Fody's dependency
                // detection can't see through that, so these need an explicit nudge (same as
                // every other collection-derived computed property in this file).
                OnPropertyChanged(nameof(PooledWaitingCount));
                OnPropertyChanged(nameof(PooledWaitMinutes));
            });
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task RefreshMyStatusAsync()
    {
        MyStatus = await _queueService.GetMyQueueStatusAsync(_businessId);
        MyWaitMinutes = MyStatus is not null
            ? await _queueService.GetEntryWaitMinutesAsync(MyStatus.EntryId)
            : null;
    }

    [RelayCommand]
    private void RequestJoin(QueueSummaryRow? row)
    {
        if (row is null) return;
        PendingJoinRow = row;
        IsPooledJoinPending = false;
        SelectedQueueService = null;
        ShowQueueServicePicker = true;
    }

    // Pooled businesses have no per-operator rows to pick from — join with "any available"
    // (null operator) and let start_serving assign a real resource later.
    [RelayCommand]
    private void RequestPooledJoin()
    {
        PendingJoinRow = null;
        IsPooledJoinPending = true;
        SelectedQueueService = null;
        ShowQueueServicePicker = true;
    }

    [RelayCommand]
    private void SelectQueueService(ServiceResponse service) => SelectedQueueService = service;

    [RelayCommand]
    private async Task ConfirmJoinAsync()
    {
        if ((PendingJoinRow is null && !IsPooledJoinPending) || SelectedQueueService is null) return;

        var row = PendingJoinRow;
        IsJoining = true;
        if (row is not null) row.IsJoining = true;
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            var customerName = await _profileService.GetMyDisplayNameAsync(Guid.Parse(userId));
            var operatorId = IsPooledJoinPending ? null : row?.OperatorId;
            await _queueService.JoinQueueAsync(
                _businessId, operatorId, Guid.Parse(userId), customerName, SelectedQueueService.Id);

            ShowQueueServicePicker = false;
            SelectedQueueService = null;
            PendingJoinRow = null;
            IsPooledJoinPending = false;

            await RefreshQueueAsync();
            await RefreshMyStatusAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsJoining = false;
            if (row is not null) row.IsJoining = false;
        }
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        if (MyStatus is null) return;

        IsLeaving = true;
        try
        {
            await _queueService.CancelEntryAsync(MyStatus.EntryId);
            MyStatus = null;
            MyWaitMinutes = null;
            await RefreshQueueAsync();
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
    private void SelectOperator(OperatorResponse op) => SelectedOperator = op;

    [RelayCommand]
    private void SelectService(ServiceResponse service)
    {
        SelectedService = service;
        SelectedDate = null;
        SelectedSlot = null;
        Slots.Clear();
        OnPropertyChanged(nameof(IsSlotsEmpty));
    }

    [RelayCommand]
    private async Task SelectDateAsync(DateTime date)
    {
        SelectedDate = date;
        SelectedSlot = null;
        await LoadSlotsAsync();
    }

    private async Task LoadSlotsAsync()
    {
        if (SelectedService is null || SelectedDate is null) return;
        if (AllowOperatorChoice && SelectedOperator is null) return;

        IsLoadingSlots = true;
        try
        {
            var slots = AllowOperatorChoice
                ? await _bookingService.GetAvailableSlotsAsync(SelectedOperator!.Id, SelectedService.Id, SelectedDate.Value)
                : await _bookingService.GetAvailableSlotsAnyAsync(_businessId, SelectedService.Id, SelectedDate.Value);

            Slots.Clear();
            foreach (var s in slots)
                Slots.Add(s);
            OnPropertyChanged(nameof(IsSlotsEmpty));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoadingSlots = false;
        }
    }

    [RelayCommand]
    private void SelectSlot(SlotResponse slot) => SelectedSlot = slot;

    [RelayCommand]
    private async Task ConfirmBookingAsync()
    {
        if (SelectedService is null || SelectedSlot is null) return;
        if (AllowOperatorChoice && SelectedOperator is null) return;

        IsConfirmingBooking = true;
        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            var booking = AllowOperatorChoice
                ? await _bookingService.CreateBookingAsync(new CreateBookingRequest
                  {
                      BusinessId = _businessId,
                      OperatorId = SelectedOperator!.Id,
                      ServiceId = SelectedService.Id,
                      CustomerId = Guid.Parse(userId),
                      StartsAt = SelectedSlot.SlotStart,
                  })
                : await _bookingService.CreateBookingAnyAsync(new CreateBookingAnyRequest
                  {
                      BusinessId = _businessId,
                      ServiceId = SelectedService.Id,
                      CustomerId = Guid.Parse(userId),
                      StartsAt = SelectedSlot.SlotStart,
                  });

            // Pooled path: the customer never picked one, so look up whichever resource the
            // server actually assigned for the confirmation/history display.
            var assignedOperatorName = AllowOperatorChoice
                ? SelectedOperator!.DisplayName
                : Operators.FirstOrDefault(o => o.Id == booking.OperatorId)?.DisplayName ?? "Any available";

            MyBookings.Insert(0, new MyBookingSummaryResponse
            {
                Id = booking.Id,
                StartsAt = booking.StartsAt,
                EndsAt = booking.EndsAt,
                Status = booking.Status,
                Operator = new VisitOperatorRef { DisplayName = assignedOperatorName },
                Service = new VisitServiceRef { Name = SelectedService.Name },
            });
            if (MyBookings.Count > 5)
                MyBookings.RemoveAt(MyBookings.Count - 1);
            OnPropertyChanged(nameof(ShowBookingHistory));

            ConfirmedOperatorName = assignedOperatorName;
            ConfirmedServiceName = SelectedService.Name;
            ConfirmedDateDisplay = SelectedSlot.SlotStart.ToOffset(TimeSpan.FromHours(2)).ToString("ddd d MMM");
            ConfirmedTimeDisplay = SelectedSlot.TimeDisplay;
            ShowConfirmationToast = true;

            SelectedOperator = Operators.Count == 1 ? Operators[0] : null;
            SelectedService = null;
            SelectedDate = null;
            SelectedSlot = null;
            Slots.Clear();
            OnPropertyChanged(nameof(IsSlotsEmpty));

            _ = HideToastAfterDelayAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            // The gist exclusion constraint caught a race — someone booked this exact slot between
            // this screen loading it and the confirm tap.
            await HandleExceptionAsync(new InvalidOperationException(
                "That slot was just booked by someone else — please pick another time."));
            await LoadSlotsAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsConfirmingBooking = false;
        }
    }

    private async Task HideToastAfterDelayAsync()
    {
        await Task.Delay(2500);
        ShowConfirmationToast = false;
    }
}
