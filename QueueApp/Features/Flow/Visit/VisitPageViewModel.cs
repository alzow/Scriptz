using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.Flow.Visit.Models;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Location;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.Flow.Visit;

public partial class VisitPageViewModel : BaseViewModel
{
    public const string QueueTable = "queue_entries";
    public const string BookingsTable = "bookings";
    public const int NoticeWindowHours = 2;
    private const int TickSeconds = 30;

    public bool IsLoading { get; set; } = true;
    public bool JustJoined { get; set; }
    public VisitRecord? Record { get; set; }
    public BusinessResponse? Business { get; set; }

    public bool HasRecord => Record is not null;
    public bool ShowMissing => Record is null && !IsLoading;
    public bool IsLive => Record?.IsLive == true;
    public bool IsSettled => Record is not null && !Record.IsLive;

    public string BusinessName => Record?.BusinessName ?? Business?.Name ?? string.Empty;
    public string StatusText => Record?.StatusText ?? string.Empty;

    // The tones the shared business header knows: accent while it is live, a plain outline once it
    // is done with, red when it went wrong.
    public string StatusTone => Record switch
    {
        null => "Muted",
        { WasNoShow: true } or { WasCancelled: true } => "Bad",
        { IsLive: true } => "Good",
        _ => "Muted",
    };
    public bool HasPhone => Business?.HasPhone == true;

    // The header's one-liner ("12 Main Rd · 181 m"); the location card wants the two halves apart.
    public string AddressLine { get; set; } = string.Empty;
    public string LocationAddress => Business?.Address ?? Business?.Suburb ?? string.Empty;
    public string DistanceText { get; set; } = string.Empty;
    public bool HasDistance => DistanceText.Length > 0;
    public bool HasLocation => LocationAddress.Length > 0 || HasDistance;

    // What the page can do about this visit, each on the page itself rather than behind a sheet.
    public bool CanShare => Record?.IsLive == true;
    public bool CanAddToCalendar => Record is { IsLive: true, IsBooking: true, SlotStart: not null };
    public bool CanLeaveQueue => Record is { IsLive: true, IsQueue: true, IsAwaitingCollection: false };
    public bool CanCancelBooking => Record is { IsLive: true, IsBooking: true, IsAwaitingCollection: false };
    public bool HasDestructiveAction => CanLeaveQueue || CanCancelBooking;
    public string DestructiveActionText => CanLeaveQueue ? "Leave the queue" : "Cancel booking";
    public bool ShowJustJoined => JustJoined && IsLive;
    public string JustJoinedText { get; set; } = string.Empty;

    public bool ShowHero => IsLive;
    public string HeroCaption { get; set; } = string.Empty;
    public string HeroTime { get; set; } = string.Empty;
    public string HeroRelative { get; set; } = string.Empty;
    public string HeroDetail { get; set; } = string.Empty;

    public string FactsTitle => IsLive ? "Your place" : "What happened";
    public ObservableCollection<VisitFactRow> Facts { get; } = new();
    public bool HasFacts => Facts.Count > 0;

    public ObservableCollection<VisitTimelineStep> Steps { get; } = new();
    public bool HasTimeline => Steps.Count > 0;

    // What the customer answered before this visit existed. Only rendered when the entry actually
    // carries answers — every visit taken before this feature, and every service that asks nothing,
    // has none and shows no section at all.
    public ObservableCollection<IntakeAnswer> IntakeAnswers { get; } = new();
    public bool HasIntakeAnswers => IntakeAnswers.Count > 0;

    public bool HasCustomerNote => Record?.HasCustomerNote == true;
    public string CustomerNoteText => Record?.CustomerNote ?? string.Empty;
    public bool HasShopNote => Record?.HasShopNote == true;
    public string ShopNoteText => Record?.ShopNoteText ?? string.Empty;

    public bool ShowReasonBlock => Record?.WasCancelled == true || Record?.WasNoShow == true;
    public string ReasonTitle { get; set; } = string.Empty;
    public string ReasonBody { get; set; } = string.Empty;
    public string ReasonQuote { get; set; } = string.Empty;

    public bool ShowPaymentLine => IsSettled && Record?.HasPrice == true;
    public string PrimaryActionText { get; set; } = string.Empty;

    private Guid _recordId;
    private VisitKind _kind;
    private bool _isVisible;
    private bool _isSubscribed;
    private double? _distanceKm;
    private IDispatcherTimer? _timer;

    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IBusinessService _businessService;
    private readonly ILocationService _locationService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;

    public VisitPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueueService queueService,
        IBookingService bookingService,
        IBusinessService businessService,
        ILocationService locationService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _queueService = queueService;
        _bookingService = bookingService;
        _businessService = businessService;
        _locationService = locationService;
        _realtimeService = realtimeService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            ReadParameters(parameters);

            _isVisible = true;
            IsLoading = true;

            var locationTask = _locationService.GetCachedLocationAsync();
            await RefreshAsync();
            await ApplyDistanceAsync(locationTask);

            RefreshView();
            StartTimer();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowMissing));
        }
    }

    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();
            _isVisible = true;
            StartTimer();
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
            StopTimer();
            await UnsubscribeRealtimeAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void ReadParameters(INavigationParameters? parameters)
    {
        try
        {
            if (parameters is null)
                throw new InvalidOperationException("VisitPage requires an entry or booking id.");

            if (parameters.TryGetValue(NavigationKeys.EntryId, out var entryId) && entryId is Guid entry)
            {
                _recordId = entry;
                _kind = VisitKind.Queue;
            }
            else if (parameters.TryGetValue(NavigationKeys.BookingId, out var bookingId) && bookingId is Guid booking)
            {
                _recordId = booking;
                _kind = VisitKind.Booking;
            }
            else
            {
                throw new InvalidOperationException("VisitPage requires an entry or booking id.");
            }

            JustJoined = parameters.TryGetValue(NavigationKeys.JustJoined, out var flag) && flag is true;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            Task<BusinessResponse?>? businessTask = null;

            Record = _kind == VisitKind.Queue
                ? await LoadEntryAsync(id => businessTask = _businessService.GetBusinessAsync(id))
                : await LoadBookingAsync();

            if (Record is null)
                return;

            businessTask ??= _businessService.GetBusinessAsync(Record.BusinessId);
            Business ??= await businessTask;
            Title = Record.BusinessName;

            if (!Record.IsLive)
                await UnsubscribeRealtimeAsync();
            else
                await SubscribeRealtimeAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task<VisitRecord?> LoadEntryAsync(Action<Guid>? onBusinessIdKnown = null)
    {
        try
        {
            var entry = await _queueService.GetEntryAsync(_recordId);
            if (entry is null)
                return null;

            var record = VisitRecord.FromEntry(entry);
            onBusinessIdKnown?.Invoke(record.BusinessId);

            if (!record.IsLive)
                return record;

            var status = await _queueService.GetMyQueueStatusAsync(record.BusinessId);
            if (status is not null && status.EntryId == entry.Id)
            {
                record.Position = status.Position;
                record.WaitMinutes = await _queueService.GetEntryWaitMinutesAsync(entry.Id);
            }

            return record;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            return null;
        }
    }

    public async Task<VisitRecord?> LoadBookingAsync()
    {
        try
        {
            var booking = await _bookingService.GetBookingAsync(_recordId);
            return booking is null ? null : VisitRecord.FromBooking(booking);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            return null;
        }
    }

    public async Task LoadDistanceAsync()
    {
        try
        {
            await ApplyDistanceAsync(_locationService.GetCachedLocationAsync());
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task ApplyDistanceAsync(Task<CustomerLocation?> locationTask)
    {
        try
        {
            if (Business?.Latitude is not { } lat || Business.Longitude is not { } lon)
                return;

            var here = await locationTask;
            if (here is null)
                return;

            _distanceKm = GeoDistance.Kilometres(here.Latitude, here.Longitude, lat, lon);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void RefreshView()
    {
        try
        {
            BuildAddressLine();
            BuildHero();
            BuildFacts();
            BuildIntake();
            BuildTimeline();
            BuildReasonBlock();
            BuildPrimaryAction();
            RaiseStateChanged();

            if (!IsLive)
                StopTimer();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void RaiseStateChanged()
    {
        try
        {
            OnPropertyChanged(nameof(HasRecord));
            OnPropertyChanged(nameof(ShowMissing));
            OnPropertyChanged(nameof(IsLive));
            OnPropertyChanged(nameof(IsSettled));
            OnPropertyChanged(nameof(BusinessName));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusTone));
            OnPropertyChanged(nameof(HasPhone));
            OnPropertyChanged(nameof(ShowJustJoined));
            OnPropertyChanged(nameof(ShowHero));
            OnPropertyChanged(nameof(FactsTitle));
            OnPropertyChanged(nameof(HasFacts));
            OnPropertyChanged(nameof(HasTimeline));
            OnPropertyChanged(nameof(HasCustomerNote));
            OnPropertyChanged(nameof(CustomerNoteText));
            OnPropertyChanged(nameof(HasShopNote));
            OnPropertyChanged(nameof(ShopNoteText));
            OnPropertyChanged(nameof(ShowReasonBlock));
            OnPropertyChanged(nameof(ShowPaymentLine));
            OnPropertyChanged(nameof(LocationAddress));
            OnPropertyChanged(nameof(HasDistance));
            OnPropertyChanged(nameof(HasLocation));
            OnPropertyChanged(nameof(CanShare));
            OnPropertyChanged(nameof(CanAddToCalendar));
            OnPropertyChanged(nameof(CanLeaveQueue));
            OnPropertyChanged(nameof(CanCancelBooking));
            OnPropertyChanged(nameof(HasDestructiveAction));
            OnPropertyChanged(nameof(DestructiveActionText));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildAddressLine()
    {
        try
        {
            var address = Business?.Address ?? Business?.Suburb ?? string.Empty;
            var distance = _distanceKm is { } km ? GeoDistance.Describe(km) : string.Empty;

            DistanceText = distance.Length > 0 ? $"{distance} away" : string.Empty;

            AddressLine = (address.Length, distance.Length) switch
            {
                (0, 0) => string.Empty,
                (0, _) => distance,
                (_, 0) => address,
                _ => $"{address} · {distance}",
            };
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildHero()
    {
        try
        {
            if (Record is not { IsLive: true } record)
                return;

            JustJoinedText = record.IsQueue
                ? "You're in. We'll keep this page up to date."
                : "Request sent. You'll hear back from the shop.";

            if (record.IsAwaitingCollection)
            {
                HeroCaption = "READY FOR COLLECTION";
                HeroTime = record.FinishedAt is { } finished ? FormatTime(finished) : "--:--";
                HeroRelative = "come by whenever you're ready";
                HeroDetail = WithWhom(record);
                return;
            }

            if (record.IsQueue)
                BuildQueueHero(record);
            else
                BuildBookingHero(record);
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildQueueHero(VisitRecord record)
    {
        try
        {
            if (record.IsBeingServed)
            {
                HeroCaption = "IN THE CHAIR SINCE";
                HeroTime = FormatTime(record.StartedAt);
                HeroRelative = record.StartedAt is { } started
                    ? $"started {DescribeSpan(DateTimeOffset.UtcNow - started)} ago"
                    : string.Empty;
                HeroDetail = WithWhom(record);
                return;
            }

            var placeText = record.Position > 0 ? $"you're {OrdinalOf(record.Position)}" : string.Empty;

            // No wait figure, which for an unassigned entry is the honest answer rather than a gap:
            // it belongs to no operator's line, so there is nothing to add up.
            var wait = record.HasOperator && record.WaitMinutes is { } minutes
                ? (int)Math.Round(minutes)
                : (int?)null;
            if (wait is null)
            {
                HeroCaption = "YOU'RE IN THE QUEUE";
                HeroTime = record.Position > 0 ? OrdinalOf(record.Position) : "--";
                HeroRelative = record.Position > 0 ? "in line" : string.Empty;
                HeroDetail = WithWhom(record);
                return;
            }

            var travel = _distanceKm is { } km ? GeoDistance.TravelMinutes(km) : (int?)null;
            var turnAt = DateTimeOffset.UtcNow.AddMinutes(wait.Value);

            if (travel is { } travelMinutes && wait.Value > travelMinutes)
            {
                var leaveAt = turnAt.AddMinutes(-travelMinutes);
                HeroCaption = "LEAVE AT";
                HeroTime = FormatTime(leaveAt);
                HeroRelative = $"in {DescribeSpan(leaveAt - DateTimeOffset.UtcNow)}";
                HeroDetail = Join($"{travelMinutes} min to get there", placeText);
                return;
            }

            HeroCaption = travel is null ? "YOUR TURN, ABOUT" : "GO NOW";
            HeroTime = FormatTime(turnAt);
            HeroRelative = $"in {DescribeSpan(TimeSpan.FromMinutes(wait.Value))}";
            HeroDetail = travel is { } near
                ? Join($"{near} min to get there", placeText)
                : placeText;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildBookingHero(VisitRecord record)
    {
        try
        {
            if (record.SlotStart is not { } slot)
                return;

            var local = LocalTime.ToLocal(slot);
            var untilSlot = slot - DateTimeOffset.UtcNow;

            HeroCaption = local.Date == LocalTime.Now.Date
                ? "YOUR SLOT TODAY"
                : LocalTime.Day(slot).ToUpperInvariant();
            HeroTime = LocalTime.Time(slot);
            HeroRelative = untilSlot > TimeSpan.Zero
                ? $"in {DescribeSpan(untilSlot)}"
                : "starting now";

            var travel = _distanceKm is { } km ? GeoDistance.TravelMinutes(km) : (int?)null;
            HeroDetail = travel is { } minutes
                ? $"{minutes} min to get there · {WithWhom(record)}"
                : WithWhom(record);
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildIntake()
    {
        try
        {
            IntakeAnswers.Clear();

            if (Record is not { } record)
                return;

            foreach (var answer in record.IntakeAnswers)
                IntakeAnswers.Add(answer);

            OnPropertyChanged(nameof(HasIntakeAnswers));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // The file itself lives in a bucket nothing has created yet, so there is nothing honest to open.
    // Saying so beats a link that does nothing.
    // TODO: stub — open the stored path once the storage bucket and its access policy exist; see
    // Documentation/service-intake-fields-backend-requirements.md.
    [RelayCommand]
    public async Task OpenIntakeFileAsync(IntakeAnswer? answer)
    {
        try
        {
            if (answer?.File is null)
                return;

            await _popupService.ShowAlertAsync(
                "Not available yet",
                $"{answer.File.Name} was attached to this visit, but file storage isn't switched on yet.");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void BuildFacts()
    {
        try
        {
            Facts.Clear();

            if (Record is not { } record)
                return;

            Facts.Add(new VisitFactRow { Label = "Service", Value = record.ServiceName, IsMono = false });
            Facts.Add(new VisitFactRow
            {
                Label = "With",
                Value = record.HasOperator ? record.OperatorName : "Whoever's free first",
                IsMono = false,
            });

            if (record.IsLive && record.IsQueue)
            {
                if (record.Position > 0)
                    Facts.Add(new VisitFactRow { Label = "Position", Value = OrdinalOf(record.Position) });

                if (record.JoinedAt is not null)
                    Facts.Add(new VisitFactRow { Label = "Joined", Value = FormatMoment(record.JoinedAt) });
            }
            else if (record.IsLive && record.IsBooking)
            {
                if (record.SlotStart is not null)
                    Facts.Add(new VisitFactRow { Label = "Slot", Value = FormatSlot(record) });
            }
            else
            {
                if (record.Waited is { } waited)
                    Facts.Add(new VisitFactRow { Label = "Waited", Value = DescribeSpan(waited) });

                if (record.Served is { } served)
                    Facts.Add(new VisitFactRow { Label = "In the chair", Value = DescribeSpan(served) });

                if (record.IsBooking && record.SlotStart is not null)
                    Facts.Add(new VisitFactRow { Label = "Slot", Value = FormatSlot(record) });
            }

            if (record.HasPrice)
                Facts.Add(new VisitFactRow { Label = "Listed price", Value = record.PriceText });
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildTimeline()
    {
        try
        {
            Steps.Clear();

            if (Record is not { } record)
                return;

            var steps = record.IsQueue ? BuildQueueTimeline(record) : BuildBookingTimeline(record);

            if (steps.Count > 0)
                steps[^1].IsLast = true;

            foreach (var step in steps)
                Steps.Add(step);
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // TODO: a no-show has no marked-at timestamp on queue_entries, so it gets no step at all
    // rather than an invented one — the reason block carries it instead.
    public List<VisitTimelineStep> BuildQueueTimeline(VisitRecord record)
    {
        var steps = new List<VisitTimelineStep>();

        try
        {
            if (record.JoinedAt is not null)
                steps.Add(Step(record.JoinedAt, "You joined the queue", VisitStepState.Done));

            // Nobody to name while the entry is still unassigned, and "Next available starts" reads
            // like a person's name on a timeline of things that happened.
            if (record.StartedAt is not null)
                steps.Add(Step(record.StartedAt,
                    record.HasOperator ? $"{record.OperatorName} started" : "Started",
                    VisitStepState.Done));
            else if (record.IsLive)
                steps.Add(Pending(record.HasOperator ? $"{record.OperatorName} starts" : "Your turn"));

            if (record.AwaitingCollectionAt is not null)
            {
                steps.Add(Step(record.AwaitingCollectionAt, "Finished — ready for collection", VisitStepState.Done));
                AddCollectedStep(steps, record);
            }
            else if (record.FinishedAt is not null)
            {
                steps.Add(Step(record.FinishedAt, "Finished", VisitStepState.Done));
            }
            else if (record.IsLive)
            {
                steps.Add(Pending("Finished"));
            }

            if (record.WasCancelled && record.CancelledAt is not null)
                steps.Add(Step(record.CancelledAt, record.CancelledByCustomer ? "You left the queue" : "The shop cancelled this", VisitStepState.Failed));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }

        return steps;
    }

    // TODO: bookings carry no marked-at for a no-show, so a no-showed booking's timeline still
    // shows only what happened up to the slot — the reason block carries the rest.
    public List<VisitTimelineStep> BuildBookingTimeline(VisitRecord record)
    {
        var steps = new List<VisitTimelineStep>();

        try
        {
            if (record.RequestedAt is not null)
                steps.Add(Step(record.RequestedAt, "You requested this time", VisitStepState.Done));

            if (record.SlotStart is { } slot)
                steps.Add(Step(record.SlotStart, "Your slot",
                    slot <= DateTimeOffset.UtcNow ? VisitStepState.Done : VisitStepState.Pending));

            // No pending variant: nothing in this app's operator flow marks a booking "started"
            // today (bookings.started_at exists on some schemas but nothing writes it yet), so a
            // step that can never resolve would be worse than one that only appears once it's true.
            if (record.StartedAt is not null)
                steps.Add(Step(record.StartedAt,
                    record.HasOperator ? $"{record.OperatorName} started" : "Started",
                    VisitStepState.Done));

            if (record.AwaitingCollectionAt is not null)
            {
                steps.Add(Step(record.AwaitingCollectionAt, "Finished — ready for collection", VisitStepState.Done));
                AddCollectedStep(steps, record);
            }

            if (record.CancelledAt is not null)
                steps.Add(Step(record.CancelledAt, record.CancelledByCustomer ? "You cancelled" : "The shop cancelled this", VisitStepState.Failed));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }

        return steps;
    }

    public void AddCollectedStep(List<VisitTimelineStep> steps, VisitRecord record)
    {
        try
        {
            if (record.CollectedAt is not null)
                steps.Add(Step(record.CollectedAt, "Collected", VisitStepState.Done));
            else if (record.IsLive)
                steps.Add(Pending("Collected"));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildReasonBlock()
    {
        try
        {
            ReasonQuote = string.Empty;

            if (Record is not { } record)
                return;

            if (record.WasNoShow)
            {
                ReasonTitle = "MARKED AS A NO-SHOW";
                ReasonBody = $"Shops mark this by hand and can get it wrong. Give {record.BusinessName} a call — they can put it right.";
                return;
            }

            if (!record.WasCancelled)
                return;

            if (record.CancelledByCustomer)
            {
                ReasonTitle = record.IsQueue ? "YOU LEFT THE QUEUE" : "YOU CANCELLED THIS";
                ReasonBody = record.IsQueue
                    ? "Nothing was charged and nothing is held. Join again whenever you like."
                    : "Nothing was charged and nothing is held. Book another time whenever you like.";
                return;
            }

            ReasonTitle = "THE SHOP CANCELLED THIS";
            ReasonBody = record.HasCancellationReason
                ? $"{record.BusinessName} called it off and gave a reason."
                : $"{record.BusinessName} called it off without giving a reason.";
            ReasonQuote = record.CancellationReason ?? string.Empty;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void BuildPrimaryAction()
    {
        try
        {
            PrimaryActionText = Record switch
            {
                null => string.Empty,
                { IsAwaitingCollection: true } => "Mark as collected",
                { IsLive: true } => string.Empty,
                { WasNoShow: true } missed => HasPhone ? $"Call {missed.BusinessName}" : string.Empty,
                { IsBooking: true } => "Book another time",
                _ => "Go again",
            };
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public async Task SubscribeRealtimeAsync()
    {
        try
        {
            if (!_isVisible || Record is not { IsLive: true } record)
                return;

            await _realtimeService.SubscribeAsync(
                this,
                "id",
                record.Id.ToString(),
                OnRealtimeChangeAsync,
                table: record.IsBooking ? BookingsTable : QueueTable);

            _isSubscribed = true;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task UnsubscribeRealtimeAsync()
    {
        try
        {
            if (!_isSubscribed)
                return;

            _isSubscribed = false;
            await _realtimeService.UnsubscribeAsync(this);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task OnRealtimeChangeAsync() =>
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await RefreshAsync();
                RefreshView();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
            }
        });

    public void StartTimer()
    {
        try
        {
            if (_timer is not null || !IsLive)
                return;

            _timer = Application.Current?.Dispatcher.CreateTimer();
            if (_timer is null)
                return;

            _timer.Interval = TimeSpan.FromSeconds(TickSeconds);
            _timer.Tick += OnTick;
            _timer.Start();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void StopTimer()
    {
        try
        {
            if (_timer is null)
                return;

            _timer.Tick -= OnTick;
            _timer.Stop();
            _timer = null;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void OnTick(object? sender, EventArgs args)
    {
        try
        {
            if (!IsLive)
            {
                StopTimer();
                return;
            }

            BuildHero();
            BuildTimeline();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // A queue entry only lacks an operator when the shop had nobody on shift to assign, and a
    // booking only until the shop picks who is taking it. Neither is a person, so neither gets
    // phrased as one.
    public static string WithWhom(VisitRecord record) => record.HasOperator
        ? $"with {record.OperatorName}"
        : record.IsQueue ? "with whoever's free first" : "with whoever's free at that time";

    public async Task<string> DescribeLeavingCostAsync(VisitRecord record)
    {
        try
        {
            if (!record.IsLive || !record.IsQueue || record.Position <= 0)
                return string.Empty;

            var summary = await _queueService.GetQueueSummaryAsync(record.BusinessId);

            // Was matched on display name, which collapses the moment a shop has two Siphos. The
            // entry carries the id; rejoining an unassigned entry lands wherever join_queue puts
            // it, so the fastest operator is the right quote for that case.
            var row = record.OperatorId is { } operatorId
                ? summary.FirstOrDefault(r => r.OperatorId == operatorId)
                : summary.FastestOperator();

            var place = $"You'd lose {OrdinalOf(record.Position)} place.";
            return row is null
                ? $"{place} Joining again puts you at the back."
                : $"{place} Joining again puts you at the back — about {(int)Math.Round(row.NewJoinWaitMinutes)} minutes at the moment.";
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            return string.Empty;
        }
    }

    [RelayCommand]
    public async Task CallShopAsync()
    {
        try
        {
            if (Business?.Phone is not { Length: > 0 } phone)
            {
                await _popupService.ShowAlertAsync("No number yet", $"{BusinessName} hasn't added a phone number.");
                return;
            }

            PhoneDialer.Default.Open(phone);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenDirectionsAsync()
    {
        try
        {
            if (Business is null)
                return;

            if (Business.Latitude is not { } lat || Business.Longitude is not { } lon)
            {
                await _popupService.ShowAlertAsync("Location not set", $"{Business.Name} hasn't added a map location yet.");
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
    public async Task AddToCalendarAsync()
    {
        try
        {
            if (Record is not { SlotStart: not null } record)
                return;

            var path = Path.Combine(FileSystem.CacheDirectory, $"visit-{record.Id}.ics");
            await File.WriteAllTextAsync(path, BuildCalendarEntry(record, Business?.Address));
            await Launcher.Default.OpenAsync(new OpenFileRequest(record.BusinessName, new ReadOnlyFile(path)));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task GoAgainAsync()
    {
        try
        {
            if (Record is not { } record)
                return;

            await NavigationService.NavigateAsync(
                record.IsBooking ? NavigationPaths.BookingFlowPage : NavigationPaths.QueueFlowPage,
                new NavigationParameters { { NavigationKeys.BusinessId, record.BusinessId } });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task PrimaryActionAsync()
    {
        try
        {
            if (Record is { IsAwaitingCollection: true })
            {
                await MarkAsCollectedAsync();
                return;
            }

            if (Record is { WasNoShow: true })
            {
                await CallShopAsync();
                return;
            }

            await GoAgainAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task MarkAsCollectedAsync()
    {
        if (Record is not { IsAwaitingCollection: true } record)
            return;

        try
        {
            if (record.IsQueue)
                await _queueService.MarkCollectedAsync(record.Id);
            else
                await _bookingService.MarkBookingCollectedAsync(record.Id);

            await RefreshAsync();
            RefreshView();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // One button, whichever ending applies — the footer shouldn't have to know which kind of visit
    // it is looking at.
    [RelayCommand]
    public async Task DestructiveActionAsync()
    {
        try
        {
            if (CanLeaveQueue)
                await LeaveQueueAsync();
            else if (CanCancelBooking)
                await CancelBookingAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task LeaveQueueAsync()
    {
        if (Record is not { IsLive: true, IsQueue: true } record)
            return;

        try
        {
            var cost = await DescribeLeavingCostAsync(record);
            var confirmed = await _popupService.ShowConfirmAsync(
                "Leave the queue?",
                string.IsNullOrEmpty(cost) ? "You'll be taken out of the line." : cost,
                accept: "Leave",
                cancel: "Stay");

            if (!confirmed)
                return;

            await _queueService.CancelEntryAsync(record.Id);
            await _queueService.StampEntryCancelledByCustomerAsync(record.Id, record.Details);

            await RefreshAsync();
            RefreshView();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task CancelBookingAsync()
    {
        if (Record is not { IsLive: true, IsBooking: true } record)
            return;

        try
        {
            var confirmed = await _popupService.ShowConfirmAsync(
                "Cancel this booking?",
                DescribeCancellationCost(record),
                accept: "Cancel booking",
                cancel: "Keep it");

            if (!confirmed)
                return;

            var booking = await _bookingService.GetBookingAsync(record.Id);
            await _bookingService.MarkCancelledByCustomerAsync(record.Id, booking?.Details);
            await _bookingService.CancelBookingAsync(record.Id);

            await RefreshAsync();
            RefreshView();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task DoneAsync()
    {
        try
        {
            await MainTabbedNavigation.ReturnToTabsAsync(NavigationService, _businessService);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public string DescribeCancellationCost(VisitRecord record)
    {
        try
        {
            if (record.SlotStart is not { } slot)
                return "The shop will see this straight away.";

            var notice = slot - DateTimeOffset.UtcNow;
            return notice < TimeSpan.FromHours(NoticeWindowHours) && notice > TimeSpan.Zero
                ? $"That's less than {NoticeWindowHours} hours before your slot — the shop may not be able to fill it. Give them a call if you can."
                : "The shop will see this straight away.";
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return string.Empty;
        }
    }

    public static string BuildShareText(VisitRecord record)
    {
        if (record.IsQueue)
            return record.Position > 0
                ? $"I'm {OrdinalOf(record.Position)} in the queue at {record.BusinessName}."
                : $"I'm in the queue at {record.BusinessName}.";

        return record.SlotStart is { } slot
            ? $"I'm booked at {record.BusinessName} on {LocalTime.Day(slot)} at {LocalTime.Time(slot)}."
            : $"I'm booked at {record.BusinessName}.";
    }

    public static string BuildCalendarEntry(VisitRecord record, string? address)
    {
        var start = record.SlotStart?.UtcDateTime ?? DateTime.UtcNow;
        var end = record.SlotEnd?.UtcDateTime ?? start.AddMinutes(30);

        return string.Join("\r\n",
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Queue//EN",
            "BEGIN:VEVENT",
            $"UID:{record.Id}",
            $"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}",
            $"DTSTART:{start:yyyyMMdd'T'HHmmss'Z'}",
            $"DTEND:{end:yyyyMMdd'T'HHmmss'Z'}",
            $"SUMMARY:{record.ServiceName} at {record.BusinessName}",
            $"LOCATION:{address ?? record.BusinessName}",
            "END:VEVENT",
            "END:VCALENDAR");
    }

    public static VisitTimelineStep Step(DateTimeOffset? at, string text, VisitStepState state) => new()
    {
        MomentText = FormatMoment(at),
        Text = text,
        State = state,
    };

    public static VisitTimelineStep Pending(string text) => new()
    {
        MomentText = "Not yet",
        Text = text,
        State = VisitStepState.Pending,
    };

    public static string FormatMoment(DateTimeOffset? instant) =>
        instant is { } value ? LocalTime.Moment(value) : "Not recorded";

    public static string FormatTime(DateTimeOffset? instant) =>
        instant is { } value ? LocalTime.Time(value) : "--:--";

    public static string FormatSlot(VisitRecord record)
    {
        if (record.SlotStart is not { } start)
            return string.Empty;

        return LocalTime.Range(start, record.SlotEnd);
    }

    public static string DescribeSpan(TimeSpan span)
    {
        var minutes = (int)Math.Round(span.TotalMinutes);
        if (minutes < 1)
            return "under a minute";

        if (minutes < 60)
            return $"{minutes} min";

        var hours = minutes / 60;
        var rest = minutes % 60;
        return rest == 0 ? $"{hours} hr" : $"{hours} hr {rest} min";
    }

    public static string Join(string first, string second) =>
        second.Length == 0 ? first : $"{first} · {second}";

    public static string OrdinalOf(int value) => value switch
    {
        11 or 12 or 13 => $"{value}th",
        _ when value % 10 == 1 => $"{value}st",
        _ when value % 10 == 2 => $"{value}nd",
        _ when value % 10 == 3 => $"{value}rd",
        _ => $"{value}th",
    };

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
        }
    }
}
