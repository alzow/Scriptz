using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Shared.Domain;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.OperatorQueue.Helpers;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Features.OperatorQueue.Sheets;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.OperatorQueue;

public partial class OperatorQueuePageViewModel : BaseViewModel
{
    public ObservableCollection<BoardSection> Sections { get; } = new();
    public ObservableCollection<QueueRowItem> PoolRows { get; } = new();
    public ObservableCollection<QueueRowItem> AwaitingCollectionRows { get; } = new();
    public bool HasAwaitingCollection => AwaitingCollectionRows.Count > 0;

    public string BusinessName { get; set; } = "Queue";
    public bool IsLoading { get; set; }

    public string WaitingCountText { get; set; } = "0";
    public string ServingCountText { get; set; } = "0";
    public string DoneTodayText { get; set; } = "0";
    public string AvgText { get; set; } = BoardConstants.EmDash;

    public bool HasPool => PoolRows.Count > 0;
    public bool IsPoolExpanded { get; set; }
    public string PoolCountText { get; set; } = string.Empty;
    public string PoolAgeText { get; set; } = string.Empty;
    public bool IsPoolUrgent { get; set; }
    public Brush PoolStroke => IsPoolUrgent ? BoardPalette.PurpleStroke : BoardPalette.PurpleDimStroke;
    public double PoolStrokeThickness => IsPoolUrgent ? 1.5 : 1;
    public string PoolChevron => IsPoolExpanded ? "ic_chevron_up" : "ic_chevron_down";

    public bool IsQuiet { get; set; }
    public string QuietText { get; set; } = string.Empty;

    public bool IsLive => true;

    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private IDispatcherTimer? _tickTimer;
    private int _ticks;

    private Guid _businessId;
    private CategoryLabelSet _labels = CategoryLabels.Resolve(null);
    private List<OperatorResponse> _operators = new();
    private List<ServiceResponse> _services = new();
    private List<QueueEntryResponse> _entries = new();
    private List<QueueSummaryRow> _summary = new();
    private bool _isVisible;
    private bool _hasAppeared;

    private readonly IQueueService _queueService;
    private readonly IBusinessService _businessService;
    private readonly IOperatorService _operatorService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;

    public OperatorQueuePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueueService queueService,
        IBusinessService businessService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IQueueRealtimeService realtimeService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _queueService = queueService;
        _businessService = businessService;
        _operatorService = operatorService;
        _serviceOfferingsService = serviceOfferingsService;
        _realtimeService = realtimeService;
        _popupService = popupService;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj)
                ? (Guid)idObj
                : await _businessService.GetOwnedBusinessIdAsync();

            // The beat StartTicking skipped because Appearing ran before this did.
            _ = _businessService.HeartbeatAsync(_businessId);

            var business = await _businessService.GetBusinessAsync(_businessId);
            BusinessName = business?.Name ?? "Queue";
            _labels = CategoryLabels.Resolve(business?.Category);

            await LoadQueueAsync();

            // Appearing fires before Loaded on Android, so the first pass through
            // OnAppearingAsync had no business id to filter on and skipped the subscription.
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
            if (!_isVisible || _businessId == Guid.Empty)
                return;

            await _realtimeService.SubscribeAsync(this, "business_id", _businessId.ToString(),
                async () => await MainThread.InvokeOnMainThreadAsync(LoadQueueAsync));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
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

            // Coming back from another tab, or from a page pushed over this one. The feed was
            // released while the board was away, so nothing has been putting changes into Sections
            // and what is on screen is as old as the moment it left.
            if (_hasAppeared)
                await LoadQueueAsync();

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

    // ── Loading ───────────────────────────────────────────────────────────────

    public async Task LoadQueueAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            IsLoading = true;

            // Five independent reads. Awaited one after another they cost five round trips of
            // latency, and this runs again on every realtime event, so the board spent most of a
            // refresh waiting rather than fetching.
            var operatorsTask = _operatorService.GetOperatorsAsync(_businessId);
            var servicesTask = _serviceOfferingsService.GetActiveServicesAsync(_businessId);
            var entriesTask = _queueService.GetActiveEntriesAsync(_businessId);
            var summaryTask = _queueService.GetQueueSummaryAsync(_businessId);
            var completedTask = SafeCompletedTodayAsync();

            _operators = await operatorsTask;
            _services = await servicesTask;
            _entries = await entriesTask;
            _summary = await summaryTask;

            var completedToday = await completedTask;
            var avg = AverageServiceMinutes(completedToday);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DoneTodayText = completedToday.Count.ToString();
                AvgText = avg is null ? BoardConstants.EmDash : $"{avg.Value:0}m";
                Rebuild();
            });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
            _loadLock.Release();
        }
    }

    public async Task<List<QueueEntryResponse>> SafeCompletedTodayAsync()
    {
        try
        {
            return await _queueService.GetCompletedTodayAsync(_businessId);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            return new List<QueueEntryResponse>();
        }
    }

    public double? AverageServiceMinutes(List<QueueEntryResponse> completed)
    {
        try
        {
            var durations = completed
                .Where(e => e.ServingAt is not null && e.DoneAt is not null)
                .Select(e => (BoardConstants.AsUtc(e.DoneAt!.Value) - BoardConstants.AsUtc(e.ServingAt!.Value)).TotalMinutes)
                .Where(minutes => minutes > 0)
                .ToList();

            return durations.Count < BoardConstants.MinimumAverageSamples ? null : durations.Average();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return null;
        }
    }

    // ── Building the board ────────────────────────────────────────────────────

    public void Rebuild()
    {
        try
        {
            Sections.Clear();

            foreach (var op in _operators.OrderBy(o => o.SortOrder))
            {
                var serving = _entries.FirstOrDefault(e => e.OperatorId == op.Id && e.Status == "serving");
                var waiting = _entries
                    .Where(e => e.OperatorId == op.Id && e.Status == "waiting")
                    .OrderBy(e => e.JoinedAt)
                    .ToList();

                var section = BuildSection(op, serving, waiting);
                Sections.Add(section);
            }

            RebuildPool();
            RebuildAwaitingCollection();
            RefreshStats();
            RefreshTickText();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public BoardSection BuildSection(OperatorResponse op, QueueEntryResponse? serving, List<QueueEntryResponse> waiting)
    {
        try
        {
            var onShift = op.IsAvailable;
            var expanded = onShift && (serving is not null || waiting.Count > 0);

            var section = new BoardSection
            {
                OperatorId = op.Id,
                Name = op.DisplayName,
                Initials = TextFormat.Initials(op.DisplayName),
                SortOrder = op.SortOrder,
                IsOnShift = onShift,
                IsExpanded = expanded,
                Serving = serving is null ? null : BuildServingCard(serving),
                StatusText = StatusTextFor(onShift, serving is not null, waiting.Count),
                StatusColor = waiting.Count > 0 ? BoardPalette.Ink : BoardPalette.Muted,
            };

            for (var i = 0; i < waiting.Count; i++)
            {
                var entry = waiting[i];
                section.Waiting.Add(new QueueRowItem
                {
                    EntryId = entry.Id,
                    OperatorId = entry.OperatorId,
                    ServiceId = entry.ServiceId,
                    CustomerName = DisplayNameOf(entry),
                    Initials = TextFormat.Initials(DisplayNameOf(entry)),
                    ServiceName = ServiceNameOf(entry.ServiceId),
                    JoinedAt = entry.JoinedAt,
                    JoinedAtText = BoardConstants.AsUtc(entry.JoinedAt).ToLocalTime().ToString("HH:mm"),
                    PositionText = (i + 1).ToString(),
                    ShowPosition = true,
                    SubText = QueueRowItem.BuildSubText(
                        ServiceNameOf(entry.ServiceId),
                        MinutesSince(entry.JoinedAt)),
                    NoteText = entry.ProgressStatus ?? string.Empty,
                    HasNote = !string.IsNullOrWhiteSpace(entry.ProgressStatus),
                    IntakeAnswers = IntakeAnswer.Ordered(entry.IntakeResponses),
                });
            }

            return section;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return new BoardSection { OperatorId = op.Id, Name = op.DisplayName };
        }
    }

    public ServingCardItem BuildServingCard(QueueEntryResponse entry)
    {
        try
        {
            var service = _services.FirstOrDefault(s => s.Id == entry.ServiceId);
            var serviceText = service is null
                ? string.Empty
                : service.PriceCents.HasValue ? $"{service.Name} · {service.PriceDisplay}" : service.Name;

            var card = new ServingCardItem
            {
                EntryId = entry.Id,
                OperatorId = entry.OperatorId,
                ServiceId = entry.ServiceId,
                CustomerName = DisplayNameOf(entry),
                ServiceText = serviceText,
                ServingAt = entry.ServingAt ?? entry.JoinedAt,
                EstimateText = service is null ? string.Empty : $"of ~{service.EstMinutes}m",
                HasEstimate = service is not null,
                NoteText = entry.ProgressStatus ?? string.Empty,
                HasNote = !string.IsNullOrWhiteSpace(entry.ProgressStatus),
                IntakeAnswers = IntakeAnswer.Ordered(entry.IntakeResponses),
            };

            card.RefreshElapsed();
            return card;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return new ServingCardItem { EntryId = entry.Id };
        }
    }

    public void RebuildPool()
    {
        try
        {
            PoolRows.Clear();

            var unassigned = _entries
                .Where(e => e.OperatorId is null)
                .OrderBy(e => e.JoinedAt)
                .ToList();

            foreach (var entry in unassigned)
            {
                var waited = MinutesSince(entry.JoinedAt);
                PoolRows.Add(new QueueRowItem
                {
                    EntryId = entry.Id,
                    OperatorId = null,
                    ServiceId = entry.ServiceId,
                    CustomerName = DisplayNameOf(entry),
                    Initials = TextFormat.Initials(DisplayNameOf(entry)),
                    ServiceName = ServiceNameOf(entry.ServiceId),
                    JoinedAt = entry.JoinedAt,
                    JoinedAtText = BoardConstants.AsUtc(entry.JoinedAt).ToLocalTime().ToString("HH:mm"),
                    ShowPosition = false,
                    SubText = QueueRowItem.BuildSubText(ServiceNameOf(entry.ServiceId), waited),
                    NoteText = entry.ProgressStatus ?? string.Empty,
                    HasNote = !string.IsNullOrWhiteSpace(entry.ProgressStatus),
                    IntakeAnswers = IntakeAnswer.Ordered(entry.IntakeResponses),
                });
            }

            var oldest = unassigned.Count == 0 ? 0 : MinutesSince(unassigned[0].JoinedAt);

            PoolCountText = $"{PoolRows.Count} unassigned";
            PoolAgeText = $"oldest waiting {oldest} min";
            IsPoolUrgent = oldest >= BoardConstants.PoolStarvationMinutes;
            OnPropertyChanged(nameof(PoolStroke));
            OnPropertyChanged(nameof(PoolStrokeThickness));

            if (PoolRows.Count == 0)
                IsPoolExpanded = false;

            OnPropertyChanged(nameof(HasPool));
            OnPropertyChanged(nameof(PoolChevron));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void RebuildAwaitingCollection()
    {
        try
        {
            AwaitingCollectionRows.Clear();

            var awaitingCollection = _entries
                .Where(e => e.IsAwaitingCollection)
                .OrderBy(e => e.AwaitingCollectionAt ?? e.DoneAt ?? e.JoinedAt)
                .ToList();

            foreach (var entry in awaitingCollection)
            {
                var readyMinutes = MinutesSince(entry.AwaitingCollectionAt ?? entry.DoneAt ?? entry.JoinedAt);

                AwaitingCollectionRows.Add(new QueueRowItem
                {
                    EntryId = entry.Id,
                    OperatorId = entry.OperatorId,
                    ServiceId = entry.ServiceId,
                    CustomerName = DisplayNameOf(entry),
                    Initials = TextFormat.Initials(DisplayNameOf(entry)),
                    ServiceName = ServiceNameOf(entry.ServiceId),
                    JoinedAt = entry.JoinedAt,
                    ShowPosition = false,
                    SubText = QueueRowItem.BuildReadySubText(ServiceNameOf(entry.ServiceId), readyMinutes),
                    IntakeAnswers = IntakeAnswer.Ordered(entry.IntakeResponses),
                });
            }

            OnPropertyChanged(nameof(HasAwaitingCollection));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void RefreshStats()
    {
        try
        {
            var waiting = _entries.Count(e => e.Status == "waiting");
            var serving = _entries.Count(e => e.Status == "serving");

            WaitingCountText = waiting.ToString();
            ServingCountText = serving.ToString();

            IsQuiet = waiting == 0 && serving == 0 && !HasAwaitingCollection;
            QuietText = $"Everyone's clear. {DoneTodayText} served today.";
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // ── The one page tick ─────────────────────────────────────────────────────

    public void StartTicking()
    {
        try
        {
            // Appearing runs before Loaded, so on the first pass there is no business to beat for
            // yet — without this the board PATCHes /businesses?id=eq.00000000-0000-0000-0000-000000000000
            // on every launch. OnLoadedAsync beats once itself as soon as the id is known.
            if (_businessId != Guid.Empty)
                _ = _businessService.HeartbeatAsync(_businessId);

            _tickTimer = Application.Current!.Dispatcher.CreateTimer();
            _tickTimer.Interval = TimeSpan.FromSeconds(BoardConstants.TickIntervalSeconds);
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

            _tickTimer.Tick -= OnTick;
            _tickTimer.Stop();
            _tickTimer = null;
            _ticks = 0;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void OnTick(object? sender, EventArgs e)
    {
        try
        {
            RefreshTickText();

            if (++_ticks % BoardConstants.HeartbeatTicks == 0 && _businessId != Guid.Empty)
                _ = _businessService.HeartbeatAsync(_businessId);
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public void RefreshTickText()
    {
        try
        {
            foreach (var section in Sections)
            {
                section.Serving?.RefreshElapsed();

                foreach (var row in section.Waiting)
                    row.RefreshWait();
            }

            foreach (var row in PoolRows)
                row.RefreshWait();

            if (PoolRows.Count == 0)
                return;

            var oldest = PoolRows.Max(r => r.WaitedMinutes);
            var ageText = $"oldest waiting {oldest} min";
            if (ageText != PoolAgeText)
                PoolAgeText = ageText;

            var urgent = oldest >= BoardConstants.PoolStarvationMinutes;
            if (urgent != IsPoolUrgent)
            {
                IsPoolUrgent = urgent;
                OnPropertyChanged(nameof(PoolStroke));
                OnPropertyChanged(nameof(PoolStrokeThickness));
            }
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task OpenSettingsAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.BusinessSettingsPage}",
                modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public void TogglePool()
    {
        try
        {
            IsPoolExpanded = !IsPoolExpanded;
            OnPropertyChanged(nameof(PoolChevron));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // Every move on an entry arrives from the sheet, whichever list the entry was standing in. The
    // row itself is only information — that is what stops a waiting entry being completed without
    // ever having been served.
    [RelayCommand]
    public async Task OpenRowSheetAsync(QueueRowItem? row)
    {
        try
        {
            if (row is null)
                return;

            await OpenEntrySheetAsync(EntrySheetRequest.FromRow(row, CanStartServing(row.OperatorId)));
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // The person in the chair opens the same sheet as everyone still in the line, so the board has
    // one interaction model rather than a card that behaves one way and rows that behave another.
    [RelayCommand]
    public async Task OpenServingSheetAsync(ServingCardItem? card)
    {
        try
        {
            if (card is null)
                return;

            var subText = string.IsNullOrWhiteSpace(card.ServiceText)
                ? _labels.ServingNowText
                : $"{card.ServiceText} · {_labels.ServingNowText}";

            await OpenEntrySheetAsync(
                EntrySheetRequest.FromServingCard(card, subText, _labels.ServingNowText));
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // A pooled entry is always startable — start_serving resolves the resource itself. An assigned
    // one waits its turn behind whoever is in that chair.
    public bool CanStartServing(Guid? operatorId)
    {
        try
        {
            return operatorId is not { } assigned
                || !_entries.Any(e => e.OperatorId == assigned && e.Status == "serving");
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return false;
        }
    }

    public async Task OpenEntrySheetAsync(EntrySheetRequest request)
    {
        try
        {
            var sheet = new EntryActionsSheet(
                _popupService,
                request,
                RequiresCollection(request.ServiceId),
                _labels.Noun);

            await _popupService.ShowSheetAsync(sheet);
            var result = await sheet.Completion;

            switch (result.Action)
            {
                case EntryAction.ServeNow:
                    await StartServingAsync(request);
                    break;

                case EntryAction.MarkDone:
                    await CompleteEntryAsync(request);
                    break;

                case EntryAction.MoveToAnotherOperator:
                    await MoveEntryAsync(request);
                    break;

                case EntryAction.MoveToEndOfQueue:
                    await MoveToEndAsync(request);
                    break;

                case EntryAction.ChangeService:
                    await ChangeServiceAsync(request);
                    break;

                case EntryAction.SaveNote:
                    await SaveNoteAsync(request, result.Note);
                    break;

                case EntryAction.MarkNoShow:
                    await ConfirmNoShowAsync(request.EntryId, request.CustomerName);
                    break;

                case EntryAction.RemoveFromQueue:
                    await ConfirmRemoveAsync(request.EntryId, request.CustomerName);
                    break;

                case EntryAction.ViewAnswers:
                    await OpenIntakeAnswersAsync(
                        request.CustomerName, request.ServiceName, request.WhenText, request.Answers);
                    break;
            }
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // On a pooled board this is also the assignment: start_serving picks a free resource itself and
    // refuses when there is none, which is the shop being busy rather than the app failing — so it
    // says so under its own title instead of the general "couldn't do that".
    public async Task StartServingAsync(EntrySheetRequest request)
    {
        try
        {
            ApplyLocally(request.EntryId, e =>
            {
                e.Status = "serving";
                e.ServingAt = DateTime.UtcNow;
            });
            await _queueService.StartServingAsync(request.EntryId);
        }
        catch (Refit.ApiException ex)
        {
            await LoadQueueAsync();
            await _popupService.ShowAlertAsync(BoardConstants.CantStartTitle, GetFriendlyErrorMessage(ex));
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    public async Task CompleteEntryAsync(EntrySheetRequest request)
    {
        try
        {
            if (RequiresCollection(request.ServiceId))
            {
                ApplyLocally(request.EntryId, e => e.Status = QueueEntryStatuses.AwaitingCollection);
                await _queueService.MarkAwaitingCollectionAsync(request.EntryId);
            }
            else
            {
                ApplyLocally(request.EntryId, e => e.Status = QueueEntryStatuses.Done);
                await _queueService.CompleteAsync(request.EntryId);
            }
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // The last inline action left on the board. Collection is a single terminal step with nothing
    // to choose, so a sheet in front of it would only be a tap in the way.
    [RelayCommand]
    public async Task MarkCollectedAsync(QueueRowItem? row)
    {
        if (row is null || row.IsBusy)
            return;

        row.IsBusy = true;
        try
        {
            ApplyLocally(row.EntryId, e => e.Status = QueueEntryStatuses.Done);
            await _queueService.MarkCollectedAsync(row.EntryId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
        finally
        {
            row.IsBusy = false;
        }
    }

    public bool RequiresCollection(Guid? serviceId)
    {
        try
        {
            return _services.FirstOrDefault(s => s.Id == serviceId)?.RequiresCollection ?? false;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return false;
        }
    }

    public async Task AssignEntryAsync(Guid entryId, Guid? operatorId)
    {
        try
        {
            ApplyLocally(entryId, e => e.OperatorId = operatorId);
            await _queueService.AssignEntryAsync(entryId, operatorId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    public async Task OpenIntakeAnswersAsync(
        string customerName,
        string serviceName,
        string whenText,
        IReadOnlyList<IntakeAnswer> answers)
    {
        try
        {
            var parameters = new NavigationParameters
            {
                [NavigationKeys.IntakeAnswers] =
                    new IntakeAnswerSnapshot(customerName, serviceName, whenText, answers),
            };

            // The board is a tab, so a plain push would bury the page inside the tab's own stack
            // with the tab bar still on screen — the same reason the flow opens modally from here.
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.IntakeAnswersPage}", parameters,
                modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    // Assignment and reassignment are the same move: a pooled entry has nowhere to go back to, an
    // assigned one does, and the sheet's own header says which of the two the operator is doing.
    public async Task MoveEntryAsync(EntrySheetRequest request)
    {
        try
        {
            var sheet = new AssignSheet(
                _popupService,
                request.CustomerName,
                request.Initials,
                request.SubText,
                OperatorQueueHelper.AssignHeaderText(request.IsInPool),
                BuildAssignTargets(request.OperatorId, includePoolOption: !request.IsInPool));

            await _popupService.ShowSheetAsync(sheet);
            var result = await sheet.Completion;

            if (result.Assigned)
                await AssignEntryAsync(request.EntryId, result.OperatorId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    public async Task MoveToEndAsync(EntrySheetRequest request)
    {
        try
        {
            ApplyLocally(request.EntryId, e => e.JoinedAt = DateTime.UtcNow);
            await _queueService.MoveEntryToEndAsync(request.EntryId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    public async Task ChangeServiceAsync(EntrySheetRequest request)
    {
        try
        {
            var sheet = new ChangeServiceSheet(
                _popupService,
                $"Change service for {request.CustomerName}",
                BuildServiceRows(request.ServiceId));

            await _popupService.ShowSheetAsync(sheet);
            var serviceId = await sheet.Completion;

            if (serviceId is not { } chosen || chosen == request.ServiceId)
                return;

            ApplyLocally(request.EntryId, e => e.ServiceId = chosen);
            await _queueService.ChangeEntryServiceAsync(request.EntryId, chosen);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    public async Task ConfirmNoShowAsync(Guid entryId, string customerName)
    {
        try
        {
            var confirmed = await _popupService.ShowConfirmAsync(
                BoardConstants.NoShowTitle, $"Mark {customerName} as a no-show?");
            if (!confirmed)
                return;

            ApplyLocally(entryId, e => e.Status = "no_show");
            await _queueService.NoShowAsync(entryId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    public async Task ConfirmRemoveAsync(Guid entryId, string customerName)
    {
        try
        {
            var confirmed = await _popupService.ShowConfirmAsync(
                BoardConstants.RemoveTitle, $"Take {customerName} out of the queue?",
                BoardConstants.RemoveConfirm, BoardConstants.RemoveCancel);
            if (!confirmed)
                return;

            ApplyLocally(entryId, e => e.Status = "cancelled");
            await _queueService.CancelEntryAsync(entryId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // The same progress_status column either way, so the customer reads one "latest update"
    // wherever the entry is. Not the same write: set_queue_progress refuses an entry that is not
    // being served, and someone still in the line is exactly who "running late" is worth saying to.
    public async Task SaveNoteAsync(EntrySheetRequest request, string? note)
    {
        try
        {
            ApplyLocally(request.EntryId, e => e.ProgressStatus = note);

            if (request.IsWaiting)
                await _queueService.SetEntryNoteAsync(request.EntryId, note);
            else
                await _queueService.SetQueueProgressAsync(request.EntryId, note);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // The queue flow, in operator mode, with the tapped column already answered for. A sheet here
    // would have meant a second service picker and no intake step at all — and join_queue rejects
    // an entry whose service left a required question unanswered, so a walk-in added without one
    // would simply stop being addable.
    [RelayCommand]
    public async Task AddToQueueAsync(BoardSection? section)
    {
        try
        {
            if (section is null)
                return;

            if (_services.Count == 0)
            {
                await _popupService.ShowAlertAsync(
                    BoardConstants.NoServicesTitle, BoardConstants.NoServicesMessage);
                return;
            }

            var parameters = new NavigationParameters
            {
                { NavigationKeys.BusinessId, _businessId },
                { NavigationKeys.OperatorId, section.OperatorId },
                { NavigationKeys.IsOperatorFlow, true },
            };

            // The board is a tab, so a plain push would bury the flow inside the tab's own stack
            // with the tab bar still on screen. Modally it gets the whole window, and comes back by
            // dismissing onto the board it left — still standing, because a modal does not replace it.
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.QueueFlowPage}", parameters,
                modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    [RelayCommand]
    public async Task ToggleShiftAsync(BoardSection? section)
    {
        if (section is null || section.IsTogglingShift)
            return;

        section.IsTogglingShift = true;
        try
        {
            await _operatorService.SetOperatorAvailableAsync(section.OperatorId, !section.IsOnShift);
            await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
        finally
        {
            section.IsTogglingShift = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public void ApplyLocally(Guid entryId, Action<QueueEntryResponse> mutate)
    {
        try
        {
            var entry = _entries.FirstOrDefault(e => e.Id == entryId);
            if (entry is null)
                return;

            mutate(entry);
            _entries = _entries.Where(e => e.Status is "waiting" or "serving" or QueueEntryStatuses.AwaitingCollection).ToList();
            Rebuild();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    public async Task ReloadAfterFailureAsync(Exception ex)
    {
        try
        {
            await LoadQueueAsync();
            await HandleExceptionAsync(ex);
        }
        catch (Exception reloadEx)
        {
            _ = HandleExceptionAsync(reloadEx);
        }
    }

    public List<AssignTargetItem> BuildAssignTargets(Guid? excludeOperatorId, bool includePoolOption)
    {
        try
        {
            var targets = new List<AssignTargetItem>();

            foreach (var op in _operators)
            {
                if (excludeOperatorId is { } excluded && op.Id == excluded)
                    continue;

                var summary = _summary.FirstOrDefault(r => r.OperatorId == op.Id);
                var wait = summary?.NewJoinWaitMinutes ?? 0;
                var ahead = (summary?.WaitingCount ?? 0) + (summary?.ServingCount ?? 0);

                targets.Add(new AssignTargetItem
                {
                    OperatorId = op.Id,
                    Name = op.DisplayName,
                    Initials = TextFormat.Initials(op.DisplayName),
                    SubLabel = !op.IsAvailable
                        ? "Off shift"
                        : wait <= 0
                            ? "Free now · starts immediately"
                            : $"{ahead} ahead · about {DateTime.Now.AddMinutes(wait):HH:mm}",
                    IsSelectable = op.IsAvailable,
                    ShowPresenceDot = op.IsAvailable,
                    SortWaitMinutes = op.IsAvailable ? wait : double.MaxValue,
                });
            }

            var ordered = targets.OrderBy(t => t.SortWaitMinutes).ThenBy(t => t.Name).ToList();

            var soonest = ordered.FirstOrDefault(t => t.IsSelectable);
            if (soonest is not null)
                soonest.ShowSoonestTag = true;

            if (includePoolOption)
            {
                ordered.Insert(0, new AssignTargetItem
                {
                    OperatorId = null,
                    Name = "Back to shared pool",
                    Initials = "★",
                    SubLabel = "Anyone can take them",
                    IsPool = true,
                    IsSelectable = true,
                });
            }

            return ordered;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return new List<AssignTargetItem>();
        }
    }

    public List<ServiceChoiceRow> BuildServiceRows(Guid? selectedServiceId)
    {
        try
        {
            return _services
                .OrderBy(s => s.SortOrder)
                .Select(s => new ServiceChoiceRow
                {
                    ServiceId = s.Id,
                    Name = s.Name,
                    MetaText = $"{s.EstMinutes} min · {s.PriceDisplay}",
                    EstMinutes = s.EstMinutes,
                    IsSelected = s.Id == selectedServiceId,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return new List<ServiceChoiceRow>();
        }
    }

    public string StatusTextFor(bool onShift, bool isServing, int waitingCount)
    {
        try
        {
            return (onShift, isServing, waitingCount) switch
            {
                (false, _, _) => "Off shift",
                (true, true, 0) => "Serving · 0 waiting",
                (true, true, 1) => "Serving · 1 waiting",
                (true, true, var n) => $"Serving · {n} waiting",
                (true, false, 0) => "Free · nobody waiting",
                (true, false, 1) => "1 waiting",
                (true, false, var n) => $"{n} waiting",
            };
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return string.Empty;
        }
    }

    public string DisplayNameOf(QueueEntryResponse entry)
    {
        try
        {
            return string.IsNullOrWhiteSpace(entry.CustomerName)
                ? BoardConstants.WalkInName
                : entry.CustomerName!;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return BoardConstants.WalkInName;
        }
    }

    public string ServiceNameOf(Guid? serviceId)
    {
        try
        {
            return _services.FirstOrDefault(s => s.Id == serviceId)?.Name ?? string.Empty;
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return string.Empty;
        }
    }

    public int MinutesSince(DateTime timestamp)
    {
        try
        {
            return (int)Math.Max(0, (DateTime.UtcNow - BoardConstants.AsUtc(timestamp)).TotalMinutes);
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
            return 0;
        }
    }


    protected override Task HandleExceptionAsync(Exception exception)
    {
        return _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
    }
}
