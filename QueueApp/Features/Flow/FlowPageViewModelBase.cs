using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.Flow.Constants;
using QueueApp.Features.Flow.Helpers;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;
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

    // The shop taking a booking at the counter walks the same steps against the same slot engine.
    // What differs is who it's for: there is no account behind it, so the name and phone are typed
    // in, and the row is inserted already confirmed instead of going through create_booking.
    public bool IsOperatorFlow { get; set; }
    public bool IsSlotFlow => IsBookingMode || IsOperatorFlow;

    public string BusinessName => Business?.Name ?? string.Empty;
    public string FlowTitle => FlowCopy.FlowTitle(IsOperatorFlow, IsBookingMode);
    public Guid BusinessId => _businessId;

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
    public string ReviewWhenText { get; set; } = string.Empty;
    public bool ShowReviewWhen => IsSlotFlow;
    public bool ShowReviewQueueLines => !IsSlotFlow;

    public bool ShowCustomerCapture => IsOperatorFlow;
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
        _selectableOperators = FlowStepEngine.SelectableOperators(_allOperators, IsOperatorFlow);

        ServiceRows.Clear();
        foreach (var service in services.OrderBy(s => s.SortOrder))
            ServiceRows.Add(ServiceChoiceItem.From(service));

        OnPropertyChanged(nameof(HasServices));
    }

    public async Task<BusinessHours> LoadHoursAsync(IReadOnlyList<OperatorResponse> operators)
    {
        try
        {
            var active = operators.Where(o => o.IsActive).ToList();
            if (active.Count == 0)
                return BusinessHours.Unknown;

            var windows = await _services.Operators.GetAvailabilityAsync(active.Select(o => o.Id).ToList());
            return BusinessHours.FromAvailability(windows);
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
                _ = ReturnToTabsAsync(NavigationPaths.BookingAgendaPage);
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
