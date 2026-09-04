using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using Refit;
using QueueApp.Constants;
using QueueApp.Features.Flow.Constants;
using QueueApp.Features.Flow.Helpers;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Flow;

public abstract partial class FlowPageViewModelBase : BaseViewModel
{
    public BusinessResponse? Business { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool IsQueueMode => Business?.Mode == FlowStepEngine.QueueMode;
    public bool IsBookingMode => Business?.Mode == FlowStepEngine.BookingMode;

    // The shop taking a booking or a walk-in at the counter walks the same steps as the customer.
    // What differs is who it's for: there is no account behind it, so the name is typed in, and the
    // row is written by the shop rather than requested from it.
    public bool IsOperatorFlow { get; set; }
    public bool IsSlotFlow => IsBookingMode;

    public string BusinessName => Business?.Name ?? string.Empty;
    public string FlowTitle => FlowCopy.FlowTitle(IsOperatorFlow, IsBookingMode);
    public Guid BusinessId => _businessId;

    // Where an operator flow came from, and the only tab it can go back to: a shop runs one manage
    // screen or the other, never both.
    public string OperatorHomeTab => IsBookingMode
        ? NavigationPaths.BookingAgendaPage
        : NavigationPaths.OperatorQueuePage;

    public ObservableCollection<RailSegment> RailSegments { get; } = new();
    public ObservableCollection<CrumbChip> Crumbs { get; } = new();
    public bool HasCrumbs => Crumbs.Count > 0;
    public string RailStepLabel { get; set; } = string.Empty;
    public string RailCountText { get; set; } = string.Empty;
    public string StepHeading { get; set; } = string.Empty;
    public string StepSubheading { get; set; } = string.Empty;
    public int CurrentStepIndex { get; set; }

    public bool ShowOperatorStep { get; set; }
    public bool ShowServiceStep { get; set; }
    public bool ShowDayStep { get; set; }
    public bool ShowTimeStep { get; set; }
    public bool ShowIntakeStep { get; set; }
    public bool ShowReviewStep { get; set; }

    public string FooterLabel { get; set; } = string.Empty;
    public string FooterValue { get; set; } = string.Empty;
    public string FooterCtaText { get; set; } = FlowConstants.NextCta;
    public bool IsFooterCtaEnabled { get; set; }
    public bool IsSubmitting { get; set; }

    public ObservableCollection<OperatorChoiceItem> OperatorChoices { get; } = new();
    public OperatorChoiceItem? SelectedOperatorChoice { get; set; }

    public ObservableCollection<ServiceChoiceItem> ServiceRows { get; } = new();
    public bool HasServices => ServiceRows.Count > 0;
    public ServiceChoiceItem? SelectedServiceRow { get; set; }

    public ObservableCollection<IntakeFieldItem> IntakeFields => _intake.Fields;
    public bool HasIntakeFields => _intake.HasFields;

    public ObservableCollection<DayChoiceItem> DayChoices { get; } = new();
    public DayChoiceItem? SelectedDay { get; set; }
    public bool IsLoadingDays { get; set; }
    public string DayFineprint { get; set; } = string.Empty;

    public SlotPeriod Morning { get; set; } = SlotPeriod.Empty(FlowConstants.MorningTitle);
    public SlotPeriod Afternoon { get; set; } = SlotPeriod.Empty(FlowConstants.AfternoonTitle);
    public SlotPeriod Evening { get; set; } = SlotPeriod.Empty(FlowConstants.EveningTitle);
    public SlotChoiceItem? SelectedSlot { get; set; }
    public bool IsLoadingSlots { get; set; }

    public ObservableCollection<QueueSummaryRow> QueueSummary { get; } = new();

    public string ReviewOperatorLabel => _labels.Noun;
    public string ReviewOperatorText { get; set; } = string.Empty;
    public string ReviewServiceText { get; set; } = string.Empty;
    public string ReviewPriceText { get; set; } = string.Empty;
    public string ReviewPositionText { get; set; } = string.Empty;
    public string ReviewTurnText { get; set; } = string.Empty;
    public string ReviewPositionLabel => FlowCopy.ReviewPositionLabel(IsOperatorFlow);
    public string ReviewTurnLabel => FlowCopy.ReviewTurnLabel(IsOperatorFlow);
    public string ReviewWhenText { get; set; } = string.Empty;
    public bool ShowReviewWhen => IsSlotFlow;
    public bool ShowReviewQueueLines => !IsSlotFlow;

    public bool ShowCustomerCapture => IsOperatorFlow;

    // bookings carries a customer_phone; queue_entries does not. Asking for a number the join has
    // nowhere to put would lose it silently, which is worse than not asking.
    public bool ShowCustomerPhone => IsOperatorFlow && IsSlotFlow;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string NoteLabelText => FlowCopy.NoteLabel(IsOperatorFlow);
    public string BookingNote { get; set; } = string.Empty;

    private Guid _businessId;
    private BusinessHours _hours = BusinessHours.Unknown;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private List<OperatorResponse> _allOperators = new();
    private List<OperatorResponse> _selectableOperators = new();
    private List<FlowStep> _steps = new();

    // What the submit just created. The visit page loads from an id rather than a handed-over
    // model, so the flow has to keep the one it was given back.
    private Guid _submittedRecordId;
    private bool _submittedIsBooking;

    // Where the agenda was standing when it handed over — the day it was showing, and the gap that
    // was tapped. Both are one-shot: once the matching chip is selected they stop steering anything.
    private DateTime? _preferredDate;
    private DateTimeOffset? _preferredStart;

    // The board column the walk-in was added from. Not one-shot: it settles the operator for the
    // whole flow, which is why the Operator step never appears.
    private Guid? _preselectedOperatorId;

    private readonly FlowServices _services;
    private readonly FlowScheduleLoader _schedule;
    private readonly FlowIntakeCoordinator _intake;
    private readonly FlowSubmissionCoordinator _submission;

    protected FlowPageViewModelBase(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        FlowServices services)
        : base(navigationService, secureStorageService)
    {
        _services = services;
        _schedule = new FlowScheduleLoader(services.Booking);
        _intake = new FlowIntakeCoordinator(services.IntakeFiles);
        _submission = new FlowSubmissionCoordinator(
            services.Booking, services.Queue, services.Auth, services.Profile);

        _intake.AnswersChanged += RefreshFooterForIntake;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            ReadNavigationParameters(parameters);

            IsLoading = true;

            var intakeFieldsTask = _services.IntakeFields.GetFieldsByServiceAsync(_businessId);
            var snapshot = TryGetSnapshot(parameters);

            var (business, services) = snapshot is not null
                ? ApplySnapshot(snapshot)
                : await LoadBusinessAsync();

            ApplyBusiness(business, services);

            var queueSummaryTask = IsQueueMode ? LoadQueueSummaryAsync() : Task.CompletedTask;
            _intake.SetCatalogue(await intakeFieldsTask);
            await queueSummaryTask;

            StartFlow();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public abstract Task OnSubmittedAsync();

    public void ReadNavigationParameters(INavigationParameters? parameters)
    {
        if (parameters is null || !parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj))
            throw new InvalidOperationException(FlowConstants.MissingBusinessIdError);

        _businessId = (Guid)idObj;

        if (parameters.TryGetValue(NavigationKeys.IsOperatorFlow, out var operatorFlagObj))
            IsOperatorFlow = operatorFlagObj is true;

        if (parameters.TryGetValue(NavigationKeys.OperatorId, out var operatorIdObj) && operatorIdObj is Guid operatorId)
            _preselectedOperatorId = operatorId;

        if (parameters.TryGetValue(NavigationKeys.PreferredDate, out var dateObj) && dateObj is DateTime date)
            _preferredDate = date.Date;

        if (parameters.TryGetValue(NavigationKeys.PreferredStart, out var startObj) && startObj is DateTimeOffset start)
            _preferredStart = start;
    }

    public static BusinessSnapshot? TryGetSnapshot(INavigationParameters? parameters) =>
        parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessSnapshot, out var snapshotObj)
            ? snapshotObj as BusinessSnapshot
            : null;

    // Handed over by the business landing, which fetched all of this a tap ago. Four round trips
    // the flow would otherwise spend re-reading what the page behind it already had on screen.
    public (BusinessResponse Business, IReadOnlyList<ServiceResponse> Services) ApplySnapshot(BusinessSnapshot snapshot)
    {
        _allOperators = snapshot.Operators.ToList();
        _hours = snapshot.Hours;
        return (snapshot.Business, snapshot.Services);
    }

    public async Task<(BusinessResponse Business, IReadOnlyList<ServiceResponse> Services)> LoadBusinessAsync()
    {
        var businessTask = _services.Business.GetBusinessAsync(_businessId);
        var operatorsTask = _services.Operators.GetOperatorsAsync(_businessId);
        var servicesTask = _services.ServiceOfferings.GetActiveServicesAsync(_businessId);

        await Task.WhenAll(businessTask, operatorsTask, servicesTask);

        var business = await businessTask
            ?? throw new InvalidOperationException(FlowConstants.BusinessGoneError);

        _allOperators = await operatorsTask;
        _hours = await LoadHoursAsync(_allOperators);

        return (business, await servicesTask);
    }

    public void ApplyBusiness(BusinessResponse business, IReadOnlyList<ServiceResponse> services)
    {
        Business = business;
        Title = business.Name;
        _labels = CategoryLabels.Resolve(business.Category);
        _selectableOperators = FlowStepEngine.NarrowToPreselected(
            FlowStepEngine.SelectableOperators(_allOperators, IsOperatorFlow), _preselectedOperatorId);

        ServiceRows.Clear();
        foreach (var service in services.OrderBy(s => s.SortOrder))
            ServiceRows.Add(ServiceChoiceItem.From(service));

        OnPropertyChanged(nameof(HasServices));
    }

    public async Task<BusinessHours> LoadHoursAsync(IReadOnlyList<OperatorResponse> operators)
    {
        try
        {
            return await _services.Operators.GetBusinessHoursAsync(operators);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
            return BusinessHours.Unknown;
        }
    }

    public async Task LoadQueueSummaryAsync()
    {
        try
        {
            var rows = await _services.Queue.GetQueueSummaryAsync(_businessId);

            QueueSummary.Clear();
            foreach (var row in rows)
                QueueSummary.Add(row);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

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
            if (CurrentStepIndex > 0)
            {
                FlowBack();
                return true;
            }

            if (!IsOperatorFlow)
                return false;

            CloseFlow();
            return true;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
            return false;
        }
    }

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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void CloseFlow()
    {
        try
        {
            ResetFlowState();

            if (IsOperatorFlow)
                _ = ReturnToTabsAsync(OperatorHomeTab);
            else
                _ = NavigationService.GoBackAsync();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void ResetFlowState()
    {
        try
        {
            _schedule.CancelPendingSlotLoad();

            BookingNote = string.Empty;
            CustomerName = string.Empty;
            CustomerPhone = string.Empty;
            ShowOperatorStep = ShowServiceStep = ShowDayStep = ShowTimeStep = false;
            ShowIntakeStep = ShowReviewStep = false;

            _intake.Clear();
            OnPropertyChanged(nameof(HasIntakeFields));

            Crumbs.Clear();
            OnPropertyChanged(nameof(HasCrumbs));
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // A push onto the modal this flow is already in, not an absolute navigation — an absolute one
    // replaces the window's root, which would take the tabbed page standing behind the modal with
    // it. Nothing is popped first either: popping destroys this page, and the push that followed
    // was being issued from a view model whose page had already gone.
    public async Task GoToVisitAsync()
    {
        try
        {
            if (_submittedRecordId == Guid.Empty)
            {
                await ReturnToTabsAsync();
                return;
            }

            var key = _submittedIsBooking ? NavigationKeys.BookingId : NavigationKeys.EntryId;

            await NavigationService.NavigateAsync(
                NavigationPaths.VisitPage,
                new NavigationParameters
                {
                    { key, _submittedRecordId },
                    { NavigationKeys.JustJoined, true },
                });
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    // The tabs are still standing behind this modal, so leaving is a dismissal and selectTab is a
    // message to the tabbed page rather than a whole shell rebuilt around the tab we want.
    public async Task ReturnToTabsAsync(string? selectTab = null)
    {
        try
        {
            await MainTabbedNavigation.ReturnToTabsAsync(
                NavigationService, _services.Business, _services.Messenger, selectTab);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public FlowStep CurrentStep => _steps.Count > 0
        ? _steps[Math.Clamp(CurrentStepIndex, 0, _steps.Count - 1)]
        : FlowStep.Service;

    public FlowCopyContext CopyContext => new(
        IsOperatorFlow,
        IsBookingMode,
        IsSlotFlow,
        _labels,
        SelectedServiceRow,
        SelectedOperatorChoice);

    public FlowStepContext StepContext => new(
        CopyContext,
        SelectedDay,
        SelectedSlot,
        _intake.OutstandingCount,
        _intake.HasFields,
        CustomerName,
        ReviewPositionText);

    public void ApplyStep()
    {
        try
        {
            var step = CurrentStep;

            ShowOperatorStep = step == FlowStep.Operator;
            ShowServiceStep = step == FlowStep.Service;
            ShowDayStep = step == FlowStep.Day;
            ShowTimeStep = step == FlowStep.Time;
            ShowIntakeStep = step == FlowStep.Intake;
            ShowReviewStep = step == FlowStep.Review;

            // The review step's footer quotes the position this computes, so it has to run first.
            if (step == FlowStep.Review)
                RefreshReview();

            ApplyChrome();
            RefreshFooter();

            if (step == FlowStep.Day)
                _ = LoadDayCountsAsync();
            else if (step == FlowStep.Time)
                _ = LoadSlotsAsync();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void ApplyChrome()
    {
        try
        {
            var chrome = FlowStepPresenter.BuildChrome(_steps, CurrentStepIndex, StepContext);

            RailStepLabel = chrome.RailStepLabel;
            RailCountText = chrome.RailCountText;
            StepHeading = chrome.Heading;
            StepSubheading = chrome.Subheading;

            RailSegments.Clear();
            foreach (var segment in chrome.Segments)
                RailSegments.Add(segment);

            Crumbs.Clear();
            foreach (var crumb in chrome.Crumbs)
                Crumbs.Add(crumb);

            OnPropertyChanged(nameof(HasCrumbs));
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void RefreshFooter()
    {
        try
        {
            var footer = FlowStepPresenter.BuildFooter(_steps, CurrentStepIndex, StepContext);

            FooterLabel = footer.Label;
            FooterValue = footer.Value;
            FooterCtaText = footer.CtaText;
            IsFooterCtaEnabled = footer.IsCtaEnabled;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // A service that asks nothing rebuilds the list it already had, SequenceEqual says so, and not
    // a single step moves. Intake goes in after Service, so the step being stood on keeps its
    // index: the customer doesn't move, the rail gains or loses a segment ahead of them.
    public void RebuildSteps()
    {
        try
        {
            if (Business is null)
                return;

            var rebuilt = FlowStepEngine.BuildSteps(
                Business, _selectableOperators, IsOperatorFlow, HasIntakeFields);

            if (rebuilt.SequenceEqual(_steps))
                return;

            _steps = rebuilt;
            ApplyStep();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void BuildOperatorChoices()
    {
        try
        {
            var choices = FlowChoiceBuilder.BuildOperatorChoices(
                _selectableOperators, QueueSummary, IsQueueMode, IsBookingMode, IsOperatorFlow);

            OperatorChoices.Clear();
            foreach (var choice in choices)
                OperatorChoices.Add(choice);

            SelectedOperatorChoice = OperatorChoices.FirstOrDefault(c => c.IsSelected);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void RefreshReview()
    {
        try
        {
            if (SelectedServiceRow is null)
                return;

            var review = FlowChoiceBuilder.BuildReview(
                SelectedServiceRow, SelectedOperatorChoice, SelectedSlot, QueueSummary, IsSlotFlow);

            ReviewOperatorText = review.OperatorText;
            ReviewServiceText = review.ServiceText;
            ReviewPriceText = review.PriceText;
            ReviewWhenText = review.WhenText;

            if (!IsSlotFlow)
            {
                ReviewPositionText = review.PositionText;
                ReviewTurnText = review.TurnText;
            }

            OnPropertyChanged(nameof(ReviewOperatorLabel));
            OnPropertyChanged(nameof(ReviewPositionLabel));
            OnPropertyChanged(nameof(ReviewTurnLabel));
            OnPropertyChanged(nameof(ShowReviewWhen));
            OnPropertyChanged(nameof(ShowReviewQueueLines));
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // Every downstream clear happens here. Nothing else sets a selection back to null.
    public void InvalidateAfter(FlowStep changed)
    {
        try
        {
            switch (changed)
            {
                // Availability is per operator; services are per business, so they survive. The
                // schedule cache is keyed by both, so nothing has to be thrown away to change either.
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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void ClearSlots()
    {
        try
        {
            SelectedSlot = null;
            Morning = SlotPeriodFor(FlowConstants.MorningTitle, FlowConstants.MorningFromHour);
            Afternoon = SlotPeriodFor(FlowConstants.AfternoonTitle, FlowConstants.AfternoonFromHour);
            Evening = SlotPeriodFor(FlowConstants.EveningTitle, FlowConstants.EveningFromHour);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
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

            _intake.BuildFor(item.Service.Id);
            OnPropertyChanged(nameof(HasIntakeFields));

            RebuildSteps();
            RefreshFooter();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
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
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void SelectIntakeOption(IntakeOptionItem? option)
    {
        try
        {
            _intake.SelectOption(option);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task PickIntakeFileAsync(IntakeFieldItem? field)
    {
        try
        {
            if (SelectedServiceRow is null)
                return;

            await _intake.PickFileAsync(field, SelectedServiceRow.Service.Id);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void ClearIntakeFile(IntakeFieldItem? field)
    {
        try
        {
            _intake.ClearFile(field);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // Typing into a short or long text field is the one answer nothing else routes through a
    // command, and the footer's CTA turns on the moment the last required one is filled.
    public void RefreshFooterForIntake(object? sender, EventArgs e)
    {
        if (ShowIntakeStep)
            RefreshFooter();
    }

    // PropertyChanged.Fody calls this off the woven setter, so the footer keeps up with the name
    // being typed on the review step.
    public void OnCustomerNameChanged() => RefreshFooter();

    public FlowScheduleKey? ScheduleKey => SelectedServiceRow is null || SelectedOperatorChoice is null
        ? null
        : new FlowScheduleKey(
            _businessId,
            SelectedOperatorChoice.OperatorId,
            SelectedServiceRow.Service.Id,
            SelectedOperatorChoice.IsAnyAvailable);

    public async Task LoadDayCountsAsync()
    {
        try
        {
            if (ScheduleKey is not { } key)
                return;

            EnsureDayStrip();
            DayFineprint = FlowCopy.DayFineprint(CopyContext);

            if (_schedule.TryGetDayCounts(key, out var cached))
            {
                ApplyDayCounts(cached);
                return;
            }

            IsLoadingDays = true;
            try
            {
                var dates = DayChoices.Select(d => d.Date).ToList();
                ApplyDayCounts(await _schedule.LoadDayCountsAsync(key, dates));
            }
            finally
            {
                IsLoadingDays = false;
            }
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public void EnsureDayStrip()
    {
        if (DayChoices.Count > 0)
            return;

        foreach (var day in FlowHelper.BuildDayStrip(FlowConstants.DayStripLength))
            DayChoices.Add(day);

        // The agenda hands over the day it was showing, so the shop doesn't re-pick it.
        if (_preferredDate is { } wanted)
        {
            SelectDay(DayChoices.FirstOrDefault(d => d.Date == wanted));
            _preferredDate = null;
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
                day.FreeText = FlowCopy.DayFreeText(count, operatorName);
            }
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public async Task LoadSlotsAsync()
    {
        try
        {
            if (ScheduleKey is not { } key || SelectedDay is null)
                return;

            var date = SelectedDay.Date;

            if (_schedule.TryGetSlots(key, date, out var cached))
            {
                ApplySlots(cached);
                return;
            }

            IsLoadingSlots = true;

            var slots = await _schedule.LoadSlotsAsync(key, date);

            // Null is a newer day selection having superseded this one. It owns the spinner from
            // here — turning it off would blank it while that newer load is still running.
            if (slots is null)
                return;

            ApplySlots(slots);
            IsLoadingSlots = false;
        }
        catch (Exception exception)
        {
            IsLoadingSlots = false;
            await HandleExceptionAsync(exception);
        }
    }

    public void ApplySlots(IReadOnlyList<SlotResponse> slots)
    {
        try
        {
            var items = slots
                .OrderBy(s => s.SlotStart)
                .Select(s => new SlotChoiceItem
                {
                    Slot = s,
                    TimeText = LocalTime.ToLocal(s.SlotStart).ToString("HH:mm"),
                })
                .ToList();

            Morning = SlotPeriodFor(
                FlowConstants.MorningTitle, FlowConstants.MorningFromHour, FlowConstants.MorningToHour, items);
            Afternoon = SlotPeriodFor(
                FlowConstants.AfternoonTitle, FlowConstants.AfternoonFromHour, FlowConstants.AfternoonToHour, items);
            Evening = SlotPeriodFor(
                FlowConstants.EveningTitle, FlowConstants.EveningFromHour, FlowConstants.EveningToHour, items);

            SelectedSlot = null;

            // A tapped gap on the agenda names an exact start. It only survives until it matches
            // once: changing the resource or the service can move every boundary on the day.
            if (_preferredStart is { } wanted)
            {
                SelectSlot(items.FirstOrDefault(i => i.Slot.SlotStart == wanted));
                _preferredStart = null;
            }

            RefreshFooter();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public SlotPeriod SlotPeriodFor(string title, int fromHour) =>
        SlotPeriod.Empty(title, FlowCopy.EmptyPeriodNote(_hours, SelectedDay?.Date, fromHour, _labels));

    public SlotPeriod SlotPeriodFor(
        string title,
        int fromHour,
        int toHour,
        IReadOnlyList<SlotChoiceItem> items) =>
        new(title,
            FlowHelper.SlotsInPeriod(items, fromHour, toHour),
            FlowCopy.EmptyPeriodNote(_hours, SelectedDay?.Date, fromHour, _labels));

    public async Task SubmitAsync()
    {
        if (SelectedServiceRow is null)
            return;

        if (IsSlotFlow && SelectedSlot is null)
            return;

        IsSubmitting = true;
        try
        {
            var request = BuildSubmissionRequest();

            var result = (IsOperatorFlow, IsBookingMode) switch
            {
                (true, true) => await _submission.SubmitOperatorBookingAsync(request),
                (true, false) => await _submission.SubmitOperatorJoinAsync(request),
                (false, true) => await _submission.SubmitBookingAsync(request),
                (false, false) => await _submission.SubmitJoinAsync(request),
            };

            _submittedRecordId = result.RecordId;
            _submittedIsBooking = result.IsBooking;

            ResetFlowState();
            await OnSubmittedAsync();
        }
        catch (ApiException exception) when (IsSlotFlow && exception.StatusCode == HttpStatusCode.Conflict)
        {
            // bookings_no_overlap caught a race — someone took this exact slot between the list
            // loading and the confirm tap.
            await HandleExceptionAsync(new InvalidOperationException(IsOperatorFlow
                ? FlowConstants.SlotTakenByShopError
                : FlowConstants.SlotTakenByCustomerError));

            _schedule.InvalidateSlots();
            await LoadSlotsAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public FlowSubmissionRequest BuildSubmissionRequest() => new()
    {
        BusinessId = _businessId,
        ServiceId = SelectedServiceRow!.Service.Id,
        OperatorId = SelectedOperatorChoice?.OperatorId,
        IsAnyAvailable = SelectedOperatorChoice?.IsAnyAvailable ?? false,
        StartsAt = SelectedSlot?.Slot.SlotStart ?? default,
        EndsAt = SelectedSlot?.Slot.SlotEnd ?? default,
        Note = TrimmedBookingNote(),
        CustomerName = CustomerName,
        CustomerPhone = CustomerPhone,
        IntakeResponses = _intake.BuildResponses(),
    };

    public string? TrimmedBookingNote() =>
        string.IsNullOrWhiteSpace(BookingNote) ? null : BookingNote.Trim();

    // Called from inside every catch block on this page, so it is the one method that must never
    // throw: an exception escaping here escapes the catch that was handling the first one, and
    // nothing above catches it.
    protected override async Task HandleExceptionAsync(Exception exception)
    {
        var message = GetFriendlyErrorMessage(exception);
        System.Diagnostics.Debug.WriteLine($"Error: {message}");

        try
        {
            await _services.Popup.ShowAlertAsync(FlowConstants.ErrorAlertTitle, message);
        }
        catch (Exception)
        {
            // No page to show it on. The line above is the whole record of it.
        }
    }
}
