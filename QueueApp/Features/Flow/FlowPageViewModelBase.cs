using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using Refit;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Auth;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Flow;

public abstract partial class FlowPageViewModelBase : BaseViewModel
{
    public BusinessResponse? Business { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool IsQueueMode => Business?.Mode == FlowStepEngine.QueueMode;
    public bool IsBookingMode => Business?.Mode == FlowStepEngine.BookingMode;

    // The shop taking a booking at the counter walks the same steps against the same slot engine.
    // What differs is who it's for: there is no account behind it, so the name and phone are typed
    // in, and the row is inserted already confirmed instead of going through create_booking.
    public bool IsOperatorFlow { get; set; }
    public bool IsSlotFlow => IsBookingMode || IsOperatorFlow;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // The three top-level states are mutually exclusive: exactly one of these renders at a time.
    public string BusinessName => Business?.Name ?? string.Empty;
    public ObservableCollection<ServiceChoiceItem> ServiceRows { get; } = new();
    public bool HasServices => ServiceRows.Count > 0;
    public string FlowTitle => IsOperatorFlow
        ? "Add a booking"
        : IsBookingMode ? "Book a slot" : "Join the queue";
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
    public int CurrentStepIndex { get; set; }
    private FlowStep CurrentStep => _steps.Count > 0
        ? _steps[Math.Clamp(CurrentStepIndex, 0, _steps.Count - 1)]
        : FlowStep.Service;

    // Flow selections
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

    // Queue data
    public ObservableCollection<QueueSummaryRow> QueueSummary { get; } = new();

    // Queue confirmation
    public string ReviewOperatorLabel => _labels.Noun;
    public string ReviewOperatorText { get; set; } = string.Empty;
    public string ReviewServiceText { get; set; } = string.Empty;
    public string ReviewPriceText { get; set; } = string.Empty;
    public string ReviewPositionText { get; set; } = string.Empty;
    public string ReviewTurnText { get; set; } = string.Empty;
    public string ReviewWhenText { get; set; } = string.Empty;
    public bool ShowReviewWhen => IsSlotFlow;
    public bool ShowReviewQueueLines => !IsSlotFlow;
    public bool ShowCustomerCapture => IsOperatorFlow;
    public string NoteLabelText => IsOperatorFlow
        ? "ADDITIONAL DETAILS — OPTIONAL"
        : "ANYTHING THEY SHOULD KNOW — OPTIONAL";

    // Free text the customer adds before committing — a registration, what is actually wrong.
    // Stored in bookings.note, which create_booking already accepts as p_note.
    public string BookingNote { get; set; } = string.Empty;
    private readonly Dictionary<(Guid OperatorId, Guid ServiceId), Dictionary<DateTime, int>> _dayCountCache = new();
    private readonly Dictionary<DateTime, List<SlotResponse>> _slotCache = new();
    private Guid _businessId;

    public Guid BusinessId => _businessId;
    private BusinessHours _hours = BusinessHours.Unknown;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private List<OperatorResponse> _allOperators = new();
    private List<OperatorResponse> _selectableOperators = new();
    private List<FlowStep> _steps = new();
    private CancellationTokenSource? _slotDebounce;

    // Where the agenda was standing when it handed over — the day it was showing, and the gap that
    // was tapped. Both are one-shot: once the matching chip is selected they stop steering anything.
    private DateTime? _preferredDate;
    private DateTimeOffset? _preferredStart;

    // Mode and page state
    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IOperatorService _operatorService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;
    private readonly IProfileService _profileService;

    protected FlowPageViewModelBase(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IBookingService bookingService,
        IAuthService authService,
        IQueuePopupService popupService,
        IProfileService profileService)
        : base(navigationService, secureStorageService)
    {
        _profileService = profileService;
        _businessService = businessService;
        _queueService = queueService;
        _operatorService = operatorService;
        _serviceOfferingsService = serviceOfferingsService;
        _bookingService = bookingService;
        _authService = authService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("A flow page requires a 'businessId' parameter.");

            if (parameters is not null)
            {
                if (parameters.TryGetValue(NavigationKeys.IsOperatorFlow, out var operatorFlagObj))
                    IsOperatorFlow = operatorFlagObj is true;

                if (parameters.TryGetValue(NavigationKeys.PreferredDate, out var dateObj) && dateObj is DateTime date)
                    _preferredDate = date.Date;

                if (parameters.TryGetValue(NavigationKeys.PreferredStart, out var startObj)
                    && startObj is DateTimeOffset start)
                    _preferredStart = start;
            }

            IsLoading = true;

            Business = await _businessService.GetBusinessAsync(_businessId)
                ?? throw new InvalidOperationException("That business is no longer available.");

            Title = Business.Name;
            _labels = CategoryLabels.Resolve(Business.Category);

            _allOperators = await _operatorService.GetOperatorsAsync(_businessId);
            _selectableOperators = FlowStepEngine.SelectableOperators(_allOperators, IsOperatorFlow);

            var services = await _serviceOfferingsService.GetActiveServicesAsync(_businessId);
            ServiceRows.Clear();
            foreach (var service in services.OrderBy(s => s.SortOrder))
                ServiceRows.Add(ServiceChoiceItem.From(service));
            OnPropertyChanged(nameof(HasServices));

            _hours = await LoadHoursAsync(_allOperators);

            if (IsQueueMode)
                await LoadQueueSummaryAsync();

            StartFlow();
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

    public async Task LoadQueueSummaryAsync()
    {
        try
        {
            var rows = await _queueService.GetQueueSummaryAsync(_businessId);

            QueueSummary.Clear();
            foreach (var row in rows)
                QueueSummary.Add(row);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void StartFlow()
    {
        try
        {
            if (Business is null)
                return;

            _steps = FlowStepEngine.BuildSteps(Business, _selectableOperators, IsOperatorFlow);

            BuildOperatorChoices();

            if (!_steps.Contains(FlowStep.Operator))
                SelectedOperatorChoice = OperatorChoices.FirstOrDefault();

            CurrentStepIndex = 0;
            ApplyStep();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public abstract Task OnSubmittedAsync();

    // Android's hardware back has to mean the same thing as the on-screen one, or it pops the whole
    // page from step three. Returns true when it consumed the press.
    //
    // Step 0 is only left to the platform for the customer, whose flow sits on top of the business
    // it came from. The operator's was pushed as a new root away from the tabs, so a plain pop has
    // nowhere to land and CloseFlow has to do it.
    public bool TryHandleHardwareBack()
    {
        try
        {
            if (CurrentStepIndex <= 0)
            {
                if (!IsOperatorFlow)
                    return false;

                CloseFlow();
                return true;
            }

            FlowBack();
            return true;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return false;
        }
    }

    // One absolute navigation, not a pop followed by a push. Popping destroys this page, and the
    // push that followed it was being issued from a view model whose page had already gone — which
    // is why the confirmation never appeared. Replacing the stack also means back from the
    // confirmation cannot walk into a flow that has already been committed.
    public async Task GoToConfirmationAsync() =>
        await NavigationService.NavigateAsync(
            $"/NavigationPage/{NavigationPaths.ConfirmationPage}",
            new NavigationParameters { { NavigationKeys.BusinessId, BusinessId } });

    // The tabs are rebuilt rather than popped back to: leaving them is an absolute navigation that
    // dropped the tabbed page, so there is no stack entry left to return to.
    public async Task ReturnToTabsAsync(string? selectTab = null)
    {
        try
        {
            var (ownsBusiness, mode) = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
            await NavigationService.NavigateAsync(
                MainTabbedNavigation.BuildMainTabbedUri(ownsBusiness, mode, selectTab));
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
    [RelayCommand]
    public void FlowBack()
    {
        try
        {
            if (CurrentStepIndex <= 0)
            {
                CloseFlow();
                return;
            }

            CurrentStepIndex--;
            ApplyStep();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    // Backing out of step 0 leaves the flow entirely, which is now a page pop rather than a state
    // toggle on the screen underneath.
    public void CloseFlow()
    {
        try
        {
            ResetFlowState();

            if (IsOperatorFlow)
                _ = ReturnToTabsAsync(NavigationPaths.BookingAgendaPage);
            else
                _ = NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // Submitting does not go through CloseFlow: OnSubmittedAsync owns the navigation from there, and
    // popping twice would take the whole stack with it.
    public void ResetFlowState()
    {
        try
        {
            BookingNote = string.Empty;
            CustomerName = string.Empty;
            CustomerPhone = string.Empty;
            ShowOperatorStep = ShowServiceStep = ShowDayStep = ShowTimeStep = ShowReviewStep = false;
            Crumbs.Clear();
            OnPropertyChanged(nameof(HasCrumbs));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    [RelayCommand]
    public async Task NextAsync()
    {
        try
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsSubmitting = false;
        }
    }
    [RelayCommand]
    public void JumpToCrumb(CrumbChip? chip)
    {
        try
        {
            if (chip is null)
                return;

            var index = _steps.IndexOf(chip.Step);
            if (index < 0)
                return;

            CurrentStepIndex = index;
            ApplyStep();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void ApplyStep()
    {
        try
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
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void ApplyStepCopy(FlowStep step)
    {
        try
        {
            switch (step)
            {
                case FlowStep.Operator:
                    StepHeading = IsOperatorFlow ? $"Which {_labels.Noun.ToLowerInvariant()}?" : _labels.StepHeading;
                    StepSubheading = IsOperatorFlow
                        ? $"Availability is per {_labels.Noun.ToLowerInvariant()}, so this decides which times are free."
                        : IsBookingMode
                            ? $"Availability is per {_labels.Noun.ToLowerInvariant()}, so this decides which times you'll see."
                            : $"Pick a {_labels.Noun.ToLowerInvariant()}, or take whoever's free first.";
                    break;
                case FlowStep.Service:
                    StepHeading = IsOperatorFlow ? "What are they in for?" : "What service do you need?";
                    StepSubheading = IsSlotFlow
                        ? "This helps us match the right appointment length."
                        : "This helps us estimate how long you'll be in the queue.";
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
                    StepHeading = IsOperatorFlow
                        ? "Who's it for?"
                        : IsBookingMode ? "Ready to request?" : "Ready to join?";
                    StepSubheading = IsOperatorFlow
                        ? "Added by you, so it's confirmed straight away. No account means no reminder — take a number if you want to call them."
                        : IsBookingMode
                            ? "The shop confirms this before it's final. You can cancel any time."
                            : "You can leave the queue any time.";
                    break;
            }
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void BuildCrumbs()
    {
        try
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
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void RefreshFooter()
    {
        try
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
                    FooterLabel = IsOperatorFlow
                        ? string.IsNullOrWhiteSpace(CustomerName) ? "Needs a name" : CustomerName.Trim()
                        : IsBookingMode ? "Requesting" : "Joining as";
                    FooterValue = IsSlotFlow ? BuildSlotRangeText() : ReviewPositionText;
                    IsFooterCtaEnabled = IsSlotFlow
                        ? SelectedServiceRow is not null && SelectedSlot is not null
                        : SelectedServiceRow is not null;
                    break;
            }

            FooterCtaText = isLast
                ? IsOperatorFlow ? "Add booking" : IsBookingMode ? "Request booking" : "Join queue"
                : "Next";
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public string BuildSlotRangeText()
    {
        try
        {
            if (SelectedSlot is null || SelectedDay is null)
                return "Pick a time";

            var start = LocalTime.ToLocal(SelectedSlot.Slot.SlotStart);
            var end = LocalTime.ToLocal(SelectedSlot.Slot.SlotEnd);
            return $"{start:ddd d} · {start:HH:mm} – {end:HH:mm}";
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return "Pick a time";
        }
    }

    // Every downstream clear happens here. Nothing else sets a selection back to null.
    public void InvalidateAfter(FlowStep changed)
    {
        try
        {
            switch (changed)
            {
                // Availability is per operator; services are per business, so they survive.
                case FlowStep.Operator:
                case FlowStep.Service:
                    SelectedDay = null;
                    foreach (var day in DayChoices)
                        day.IsSelected = false;

                    // The cache is keyed by date alone, so it is only valid for one operator/service
                    // pair — both of these change which slots a date has, so it has to go.
                    _slotCache.Clear();
                    ClearSlots();
                    break;

                // A different day is a different cache key, not a stale one: keep what's already
                // fetched so stepping back to an earlier day doesn't re-hit the RPC.
                case FlowStep.Day:
                    ClearSlots();
                    break;
            }
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void ClearSlots()
    {
        try
        {
            SelectedSlot = null;
            Morning = new SlotPeriod("MORNING", Array.Empty<SlotChoiceItem>(), "none");
            Afternoon = new SlotPeriod("AFTERNOON", Array.Empty<SlotChoiceItem>(), "none");
            Evening = new SlotPeriod("EVENING", Array.Empty<SlotChoiceItem>(), "none");
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    [RelayCommand]
    public void SelectOperator(OperatorChoiceItem? item)
    {
        try
        {
            if (item is null || ReferenceEquals(item, SelectedOperatorChoice))
                return;

            foreach (var choice in OperatorChoices)
                choice.IsSelected = ReferenceEquals(choice, item);

            SelectedOperatorChoice = item;
            InvalidateAfter(FlowStep.Operator);
            RefreshFooter();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    [RelayCommand]
    public void SelectService(ServiceChoiceItem? item)
    {
        try
        {
            if (item is null || ReferenceEquals(item, SelectedServiceRow))
                return;

            foreach (var row in ServiceRows)
                row.IsSelected = ReferenceEquals(row, item);

            SelectedServiceRow = item;
            InvalidateAfter(FlowStep.Service);
            RefreshFooter();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    [RelayCommand]
    public void SelectDay(DayChoiceItem? item)
    {
        try
        {
            if (item is null || !item.IsSelectable || ReferenceEquals(item, SelectedDay))
                return;

            foreach (var day in DayChoices)
                day.IsSelected = ReferenceEquals(day, item);

            SelectedDay = item;
            InvalidateAfter(FlowStep.Day);
            RefreshFooter();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    [RelayCommand]
    public void SelectSlot(SlotChoiceItem? item)
    {
        try
        {
            if (item is null)
                return;

            foreach (var slot in Morning.Slots.Concat(Afternoon.Slots).Concat(Evening.Slots))
                slot.IsSelected = ReferenceEquals(slot, item);

            SelectedSlot = item;
            RefreshFooter();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public void BuildOperatorChoices()
    {
        try
        {
            OperatorChoices.Clear();

            // The shop gets no "any available": get_available_slots_any returns a time, not the resource
            // it belongs to, and an operator-created booking is a direct insert that needs a real
            // operator_id. So the pooled choice is customer-only, and the shop picks a real resource.
            //
            // queue_entries.operator_id is nullable, so "any available" is a real first-class choice
            // there — pinned, tagged, and selected by default so the common path is one tap.
            if (IsQueueMode && !IsOperatorFlow)
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
            else if (IsBookingMode && !IsOperatorFlow)
            {
                var any = new OperatorChoiceItem
                {
                    OperatorId = null,
                    Name = "Any available",
                    Initials = "★",
                    SubLabel = "Whoever's free at that time",
                    IsAnyAvailable = true,
                    ShowFastestTag = false,
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
                    : summary is null
                        ? "Free now · about 0 min"
                        : (summary.WaitingCount, summary.ServingCount) switch
                        {
                            (0, 0) => $"Free now · about {summary.NewJoinWaitMinutes:0} min",
                            (var waiting, 0) => $"{waiting} waiting · about {summary.NewJoinWaitMinutes:0} min",
                            (0, var serving) => $"{serving} being served · about {summary.NewJoinWaitMinutes:0} min",
                            (var waiting, var serving) =>
                                $"{waiting} waiting · {serving} being served · about {summary.NewJoinWaitMinutes:0} min",
                        };

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
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public async Task LoadDayCountsAsync()
    {
        try
        {
            if (SelectedOperatorChoice is null || SelectedServiceRow is null)
                return;

            var operatorId = SelectedOperatorChoice.OperatorId;
            var isAny = SelectedOperatorChoice.IsAnyAvailable;

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

                // The agenda hands over the day it was showing, so the shop doesn't re-pick it.
                if (_preferredDate is { } wanted)
                {
                    SelectDay(DayChoices.FirstOrDefault(d => d.Date == wanted));
                    _preferredDate = null;
                }
            }

            // Pooled counts are shop-wide (get_available_slots_any), so no "whose slots" caveat is
            // needed there — only the single-operator path needs to say whose free time it's counting.
            DayFineprint = isAny
                ? SelectedServiceRow.Service.EstMinutes >= 120
                    ? $"A {SelectedServiceRow.DurationText} job needs one unbroken block, so some days show fewer options."
                    : "Counts are the shop's free slots across everyone."
                : SelectedServiceRow.Service.EstMinutes >= 120
                    ? $"A {SelectedServiceRow.DurationText} job needs one unbroken block, so some days show fewer options than {SelectedOperatorChoice.Name} has slots."
                    : $"Counts are {SelectedOperatorChoice.Name}'s free slots, not the whole shop's.";

            var serviceId = SelectedServiceRow.Service.Id;
            var key = (operatorId ?? Guid.Empty, serviceId);

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
                    var slots = isAny
                        ? await _bookingService.GetAvailableSlotsAnyAsync(_businessId, serviceId, date)
                        : await _bookingService.GetAvailableSlotsAsync(operatorId!.Value, serviceId, date);
                    return (date, count: slots.Count);
                }));

                var counts = results.ToDictionary(r => r.date, r => r.count);
                _dayCountCache[key] = counts;
                ApplyDayCounts(counts);
            }
            finally
            {
                IsLoadingDays = false;
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    public void ApplyDayCounts(IReadOnlyDictionary<DateTime, int> counts)
    {
        try
        {
            var operatorName = SelectedOperatorChoice?.Name ?? string.Empty;

            foreach (var day in DayChoices)
            {
                var count = counts.TryGetValue(day.Date, out var value) ? value : 0;
                day.IsFull = count == 0;
                day.FreeText = count == 0 ? "full" : $"{count} free · {operatorName}";
            }
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public async Task LoadSlotsAsync()
    {
        if (SelectedOperatorChoice is null
            || SelectedServiceRow is null
            || SelectedDay is null)
            return;

        var operatorId = SelectedOperatorChoice.OperatorId;
        var isAny = SelectedOperatorChoice.IsAnyAvailable;
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

            var slots = isAny
                ? await _bookingService.GetAvailableSlotsAnyAsync(_businessId, SelectedServiceRow.Service.Id, date)
                : await _bookingService.GetAvailableSlotsAsync(operatorId!.Value, SelectedServiceRow.Service.Id, date);

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
    public void ApplySlots(IReadOnlyList<SlotResponse> slots)
    {
        try
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

            // A tapped gap on the agenda names an exact start. It only survives until it matches once:
            // changing the resource or the service can move every boundary on the day.
            if (_preferredStart is { } wanted)
            {
                SelectSlot(items.FirstOrDefault(i => i.Slot.SlotStart == wanted));
                _preferredStart = null;
            }

            RefreshFooter();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public static List<SlotChoiceItem> InPeriod(IEnumerable<SlotChoiceItem> items, int fromHour, int toHour) =>
        items.Where(i =>
        {
            var hour = LocalTime.ToLocal(i.Slot.SlotStart).Hour;
            return hour >= fromHour && hour < toHour;
        }).ToList();

    // An absent period needs explaining or it reads as a bug — a three-hour job at a shop that shuts
    // at 17:00 genuinely has no evening, and saying so is the difference between the two.
    public string EmptyNote(int fromHour, int toHour)
    {
        try
        {
            if (SelectedDay is null)
                return "none";

            if (_hours.ClosingTimeOn(SelectedDay.Date) is { } closing && closing.TotalHours <= fromHour)
                return $"none — shop closes {BusinessHours.FormatClock(closing)}";

            return "none — nothing long enough left";
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return "none";
        }
    }
    public string? TrimmedBookingNote() =>
        string.IsNullOrWhiteSpace(BookingNote) ? null : BookingNote.Trim();
    public void RefreshReview()
    {
        try
        {
            if (SelectedServiceRow is null)
                return;

            ReviewOperatorText = SelectedOperatorChoice?.Name ?? "Any available";
            ReviewServiceText = $"{SelectedServiceRow.Name} · {SelectedServiceRow.DurationText}";
            ReviewPriceText = SelectedServiceRow.PriceText;

            OnPropertyChanged(nameof(ShowReviewWhen));
            OnPropertyChanged(nameof(ShowReviewQueueLines));

            if (IsBookingMode)
            {
                ReviewWhenText = BuildSlotRangeText();
                OnPropertyChanged(nameof(ReviewOperatorLabel));
                return;
            }

            var row = SelectedOperatorChoice?.OperatorId is { } operatorId
                ? QueueSummary.FirstOrDefault(r => r.OperatorId == operatorId)
                : QueueSummary.OrderBy(r => r.NewJoinWaitMinutes).FirstOrDefault();

            var ahead = row is null ? 0 : row.WaitingCount + row.ServingCount;
            var waitMinutes = row?.NewJoinWaitMinutes ?? 0;

            ReviewPositionText = Ordinal(ahead + 1) + " in line";
            var turnAt = LocalTime.Now.AddMinutes(waitMinutes);
            ReviewTurnText = turnAt.ToString("HH:mm");

            OnPropertyChanged(nameof(ReviewOperatorLabel));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }
    public Task SubmitAsync() => IsOperatorFlow
        ? SubmitOperatorBookingAsync()
        : IsBookingMode ? SubmitBookingAsync() : SubmitJoinAsync();

    // PropertyChanged.Fody calls this off the woven setter, so the footer keeps up with the name
    // being typed on the review step.
    public void OnCustomerNameChanged() => RefreshFooter();

    // The shop's own booking is a direct insert, not create_booking: there is no customer_id to
    // supply and nobody left to confirm with, so it goes in already confirmed with whatever name
    // and number the operator was given.
    public async Task SubmitOperatorBookingAsync()
    {
        if (SelectedServiceRow is null || SelectedSlot is null)
            return;

        if (SelectedOperatorChoice?.OperatorId is not { } operatorId)
        {
            await HandleExceptionAsync(new InvalidOperationException(
                $"Pick which {_labels.Noun.ToLowerInvariant()} is taking this before adding it."));
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            await HandleExceptionAsync(new InvalidOperationException(
                "A name is needed — it's all the agenda has to show for a booking with no account behind it."));
            return;
        }

        IsSubmitting = true;
        try
        {
            await _bookingService.CreateOperatorBookingAsync(new CreateOperatorBookingRequest
            {
                BusinessId = _businessId,
                OperatorId = operatorId,
                ServiceId = SelectedServiceRow.Service.Id,
                StartsAt = SelectedSlot.Slot.SlotStart,
                EndsAt = SelectedSlot.Slot.SlotEnd,
                Status = BookingStatuses.Confirmed,
                Note = TrimmedBookingNote(),
                CustomerName = CustomerName.Trim(),
                CustomerPhone = string.IsNullOrWhiteSpace(CustomerPhone) ? null : CustomerPhone.Trim(),
                Details = new BookingDetails { CreatedBy = "operator" },
            });

            ResetFlowState();
            await OnSubmittedAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            await HandleExceptionAsync(new InvalidOperationException(
                "That slot was just taken — please pick another time."));
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
    public async Task SubmitJoinAsync()
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

            ResetFlowState();
            await OnSubmittedAsync();
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
    // create_booking has no name parameter and the shop cannot read the customer's profile, so the
    // booking lands on the agenda as "Customer". The customer owns the row they just created, so
    // they write their own name onto it. Best effort: a booking with no name on it beats failing a
    // booking that already succeeded.
    public async Task StampBookingCustomerNameAsync(Guid bookingId, Guid userId)
    {
        try
        {
            var profile = await _profileService.GetMyProfileAsync(userId);
            if (profile is null || string.IsNullOrWhiteSpace(profile.DisplayName))
                return;

            await _bookingService.SetCustomerNameAsync(bookingId, profile.DisplayName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not stamp the booking's customer name: {ex.Message}");
        }
    }

    public async Task SubmitBookingAsync()
    {
        if (SelectedServiceRow is null || SelectedSlot is null)
            return;

        if (SelectedOperatorChoice is null)
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

            BookingResponse booking;

            if (SelectedOperatorChoice.IsAnyAvailable)
            {
                booking = await _bookingService.CreateBookingAnyAsync(new CreateBookingAnyRequest
                {
                    BusinessId = _businessId,
                    ServiceId = SelectedServiceRow.Service.Id,
                    CustomerId = Guid.Parse(userId),
                    StartsAt = SelectedSlot.Slot.SlotStart,
                    Note = TrimmedBookingNote(),
                });
            }
            else
            {
                booking = await _bookingService.CreateBookingAsync(new CreateBookingRequest
                {
                    BusinessId = _businessId,
                    OperatorId = SelectedOperatorChoice.OperatorId!.Value,
                    ServiceId = SelectedServiceRow.Service.Id,
                    CustomerId = Guid.Parse(userId),
                    StartsAt = SelectedSlot.Slot.SlotStart,
                    Note = TrimmedBookingNote(),
                });
            }

            await StampBookingCustomerNameAsync(booking.Id, Guid.Parse(userId));

            ResetFlowState();
            await OnSubmittedAsync();
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
    // Static, so there is no HandleExceptionAsync to reach — an avatar that reads "?" is the whole
    // consequence of a name this can't make initials out of.
    public static string Initials(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
        catch (Exception)
        {
            return "?";
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
