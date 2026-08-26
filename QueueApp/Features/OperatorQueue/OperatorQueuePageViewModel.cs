using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.OperatorQueue.Models;
using QueueApp.Features.OperatorQueue.Sheets;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.OperatorQueue;

// The shop board. One phone, on a counter, picked up by whoever's hands are free.
//
// Everything here is a client-side grouping of one stream: _entries is the board's source of
// truth, Rebuild() derives the sections and the pool from it, and every mutation updates _entries
// first so the screen moves on the tap rather than on the round trip. The realtime event that
// follows re-reads and rebuilds, which is also what puts the board right if a call failed.
public partial class OperatorQueuePageViewModel : BaseViewModel
{
    private readonly IQueueService _queueService;
    private readonly IBusinessService _businessService;
    private readonly IOperatorService _operatorService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IQueueRealtimeService _realtimeService;
    private readonly IQueuePopupService _popupService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    // One timer for the whole page. Not one per section — five sections would otherwise mean five
    // timers ticking five times a second between them.
    private IDispatcherTimer? _tickTimer;
    private int _ticks;

    private Guid _businessId;
    private List<OperatorResponse> _operators = new();
    private List<ServiceResponse> _services = new();
    private List<QueueEntryResponse> _entries = new();
    private List<QueueSummaryRow> _summary = new();

    public ObservableCollection<BoardSection> Sections { get; } = new();
    public ObservableCollection<QueueRowItem> PoolRows { get; } = new();

    public string BusinessName { get; set; } = "Queue";
    public bool IsLoading { get; set; }

    // ── Shop stats ────────────────────────────────────────────────────────────
    public string WaitingCountText { get; set; } = "0";
    public string ServingCountText { get; set; } = "0";
    public string DoneTodayText { get; set; } = "0";

    // Em-dash rather than an invented number: operator_avg_minutes returns null until an operator
    // has enough completed services behind them, and a made-up average is worse than no average.
    public string AvgText { get; set; } = BoardConstants.EmDash;

    // ── Shared pool ───────────────────────────────────────────────────────────
    public bool HasPool => PoolRows.Count > 0;
    public bool IsPoolExpanded { get; set; }
    public string PoolCountText { get; set; } = string.Empty;
    public string PoolAgeText { get; set; } = string.Empty;

    // Past the starvation threshold the banner border goes to full purple. Nothing forces anyone
    // to take from the pool — a customer chose "any available" because it promised the shortest
    // wait and can sit there while both barbers work their own lists. This readout is the minimum
    // honest response to that.
    public bool IsPoolUrgent { get; set; }

    // Precomputed rather than run through a converter, so the banner's border is a plain property
    // read. Full purple past the threshold; the dim edge below it.
    public Brush PoolStroke => IsPoolUrgent ? BoardPalette.PurpleStroke : BoardPalette.PurpleDimStroke;
    public double PoolStrokeThickness => IsPoolUrgent ? 1.5 : 1;
    public string PoolChevron => IsPoolExpanded ? "ic_chevron_up" : "ic_chevron_down";

    // ── Quiet / paused ────────────────────────────────────────────────────────
    public bool IsQuiet { get; set; }
    public string QuietText { get; set; } = string.Empty;

    // Rendered, deliberately not wired. There is no column behind a shop pause: businesses.is_active
    // delists the business entirely, which is a different thing from "nobody new can join right
    // now". Flagged in the spec's open items; when a real column lands, this is the binding.
    public bool IsPaused => false;
    public bool IsLive => !IsPaused;

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

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var idObj)
                ? (Guid)idObj
                : await _businessService.GetOwnedBusinessIdAsync();

            var business = await _businessService.GetBusinessAsync(_businessId);
            BusinessName = business?.Name ?? "Queue";

            await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        // One subscription for the page, scoped to business_id. Every section — the pool included
        // — is a grouping of this one stream, so there is nothing to subscribe per operator.
        // The table goes through the parameterised argument rather than being hardcoded here.
        await _realtimeService.SubscribeAsync("business_id", _businessId.ToString(),
            async () => await MainThread.InvokeOnMainThreadAsync(LoadQueueAsync));

        StartTicking();
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        StopTicking();
        await _realtimeService.UnsubscribeAsync();
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    private async Task LoadQueueAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            IsLoading = true;

            _operators = await _operatorService.GetOperatorsAsync(_businessId);
            _services = await _serviceOfferingsService.GetActiveServicesAsync(_businessId);
            _entries = await _queueService.GetActiveEntriesAsync(_businessId);
            _summary = await _queueService.GetQueueSummaryAsync(_businessId);

            var doneToday = await SafeDoneTodayAsync();
            var avg = await SafeAverageMinutesAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DoneTodayText = doneToday.ToString();
                AvgText = avg is null ? BoardConstants.EmDash : $"{avg.Value:0}m";
                Rebuild();
            });
        }
        finally
        {
            IsLoading = false;
            _loadLock.Release();
        }
    }

    // The stats strip is decoration on top of a working board: a shop that hasn't run these
    // functions yet, or an RPC that isn't deployed, must not take the queue down with it.
    private async Task<int> SafeDoneTodayAsync()
    {
        try
        {
            return await _queueService.GetCompletedTodayCountAsync(_businessId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Board] done-today unavailable: {ex.Message}");
            return 0;
        }
    }

    // Shop-level average across the operators on shift. operator_avg_minutes is per operator and
    // already carries its own count(*) >= 3 guard, so operators without enough history simply
    // don't contribute — and when none of them do, the tile stays an em-dash.
    private async Task<decimal?> SafeAverageMinutesAsync()
    {
        try
        {
            var onShift = _operators.Where(o => o.IsAvailable).ToList();
            if (onShift.Count == 0)
                return null;

            var averages = await Task.WhenAll(onShift.Select(o => _queueService.GetOperatorAvgMinutesAsync(o.Id)));
            var known = averages.Where(a => a.HasValue).Select(a => a!.Value).ToList();

            return known.Count == 0 ? null : known.Average();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Board] average unavailable: {ex.Message}");
            return null;
        }
    }

    // ── Building the board ────────────────────────────────────────────────────

    // Operators render in sort_order regardless of state — only heights move. Not by busyness, not
    // by urgency, not by who's serving. A barber should never have to look for himself.
    private void Rebuild()
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
        RefreshStats();
        RefreshTickText();
    }

    private BoardSection BuildSection(OperatorResponse op, QueueEntryResponse? serving, List<QueueEntryResponse> waiting)
    {
        var onShift = op.IsAvailable;
        var expanded = onShift && (serving is not null || waiting.Count > 0);

        var section = new BoardSection
        {
            OperatorId = op.Id,
            Name = op.DisplayName,
            Initials = InitialsOf(op.DisplayName),
            SortOrder = op.SortOrder,
            IsOnShift = onShift,
            IsExpanded = expanded,
            Serving = serving is null ? null : BuildServingCard(serving),
            StatusText = StatusTextFor(onShift, serving is not null, waiting.Count),
            // Ink when somebody is waiting, muted when nobody is. Never purple — purple on this
            // screen belongs to the unassigned pool and nothing else.
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
                Initials = InitialsOf(DisplayNameOf(entry)),
                ServiceName = ServiceNameOf(entry.ServiceId),
                JoinedAt = entry.JoinedAt,
                JoinedAtText = BoardConstants.AsUtc(entry.JoinedAt).ToLocalTime().ToString("HH:mm"),
                PositionText = (i + 1).ToString(),
                ShowPosition = true,
                // Only the top row carries an inline Serve. Everything below it opens the sheet,
                // which is where anything destructive lives.
                ShowServe = i == 0,
                SubText = QueueRowItem.BuildSubText(
                    ServiceNameOf(entry.ServiceId),
                    MinutesSince(entry.JoinedAt)),
                SectionIsServing = serving is not null,
            });
        }

        return section;
    }

    private ServingCardItem BuildServingCard(QueueEntryResponse entry)
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
            // Falls back to joined_at when serving_at hasn't come back yet, so the card always has
            // a timer rather than a blank where one should be.
            ServingAt = entry.ServingAt ?? entry.JoinedAt,
            EstimateText = service is null ? string.Empty : $"of ~{service.EstMinutes}m",
            HasEstimate = service is not null,
            NoteText = string.IsNullOrWhiteSpace(entry.ProgressStatus) ? "Add a note" : entry.ProgressStatus!,
            HasNote = !string.IsNullOrWhiteSpace(entry.ProgressStatus),
        };

        card.RefreshElapsed();
        return card;
    }

    // The pool is a banner, not a column: it isn't a peer of the barbers, it's an exception to be
    // cleared. Absent when empty — not an empty state.
    private void RebuildPool()
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
                Initials = InitialsOf(DisplayNameOf(entry)),
                ServiceName = ServiceNameOf(entry.ServiceId),
                JoinedAt = entry.JoinedAt,
                JoinedAtText = BoardConstants.AsUtc(entry.JoinedAt).ToLocalTime().ToString("HH:mm"),
                ShowPosition = false,
                ShowAssign = true,
                SubText = QueueRowItem.BuildSubText(ServiceNameOf(entry.ServiceId), waited),
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

    private void RefreshStats()
    {
        var waiting = _entries.Count(e => e.Status == "waiting");
        var serving = _entries.Count(e => e.Status == "serving");

        WaitingCountText = waiting.ToString();
        ServingCountText = serving.ToString();

        IsQuiet = waiting == 0 && serving == 0;
        QuietText = $"Everyone's clear. {DoneTodayText} served today.";
    }

    // ── The one page tick ─────────────────────────────────────────────────────

    private void StartTicking()
    {
        _ = _businessService.HeartbeatAsync(_businessId);

        _tickTimer = Application.Current!.Dispatcher.CreateTimer();
        _tickTimer.Interval = TimeSpan.FromSeconds(BoardConstants.TickIntervalSeconds);
        _tickTimer.Tick += OnTick;
        _tickTimer.Start();
    }

    private void StopTicking()
    {
        if (_tickTimer is null)
            return;

        _tickTimer.Tick -= OnTick;
        _tickTimer.Stop();
        _tickTimer = null;
        _ticks = 0;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        RefreshTickText();

        // The presence heartbeat rides the same timer rather than running one of its own.
        if (++_ticks % BoardConstants.HeartbeatTicks == 0)
            _ = _businessService.HeartbeatAsync(_businessId);
    }

    // Only touches text that has actually changed — the items guard their own setters, so a card
    // whose minute hasn't turned over doesn't re-notify and doesn't re-layout.
    private void RefreshTickText()
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

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.BusinessSettingsPage);
    }

    [RelayCommand]
    private void TogglePool()
    {
        IsPoolExpanded = !IsPoolExpanded;
        OnPropertyChanged(nameof(PoolChevron));
    }

    // Done is the only filled green on the board, and it is the action pressed forty times a day.
    // It deliberately does not auto-advance: on a shared counter phone the next customer usually
    // isn't in the chair yet, and auto-starting would run the clock on someone standing outside.
    [RelayCommand]
    private async Task DoneAsync(ServingCardItem? card)
    {
        if (card is null || card.IsBusy)
            return;

        card.IsBusy = true;
        try
        {
            ApplyLocally(card.EntryId, e => e.Status = "completed");
            await _queueService.CompleteAsync(card.EntryId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
        finally
        {
            card.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ServeAsync(QueueRowItem? row)
    {
        if (row is null || row.IsBusy)
            return;

        row.IsBusy = true;
        try
        {
            ApplyLocally(row.EntryId, e =>
            {
                e.Status = "serving";
                e.ServingAt = DateTime.UtcNow;
            });
            await _queueService.StartServingAsync(row.EntryId);
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

    // Assigning and serving are different acts. This sets operator_id and stops there — with a
    // shared counter phone the app cannot know who's holding it, which is why the sheet asks.
    [RelayCommand]
    private async Task AssignAsync(QueueRowItem? row)
    {
        if (row is null || row.IsBusy)
            return;

        // The row may have been rebuilt out from under this tap by a realtime event.
        if (_entries.All(e => e.Id != row.EntryId))
            return;

        var sheet = new AssignSheet(
            _popupService,
            row.CustomerName,
            row.Initials,
            $"{row.ServiceName} · any available · waiting {row.WaitedMinutes}m",
            "WHO'S TAKING THIS ONE?",
            showNoShow: true,
            BuildAssignTargets(excludeOperatorId: null, includePoolOption: false));

        await _popupService.ShowSheetAsync(sheet);
        var result = await sheet.Completion;

        if (result.MarkNoShow)
        {
            await ConfirmNoShowAsync(row.EntryId, row.CustomerName);
            return;
        }

        if (!result.Assigned)
            return;

        await AssignEntryAsync(row, result.OperatorId);
    }

    private async Task AssignEntryAsync(QueueRowItem row, Guid? operatorId)
    {
        row.IsBusy = true;
        try
        {
            ApplyLocally(row.EntryId, e => e.OperatorId = operatorId);
            await _queueService.AssignEntryAsync(row.EntryId, operatorId);
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

    [RelayCommand]
    private async Task OpenRowActionsAsync(QueueRowItem? row)
    {
        if (row is null)
            return;

        var sectionIsServing = row.OperatorId is { } opId
            && _entries.Any(e => e.OperatorId == opId && e.Status == "serving");

        var sheet = new EntryActionsSheet(
            _popupService,
            row.CustomerName,
            row.Initials,
            $"{row.ServiceName} · joined {row.JoinedAtText} · waiting {row.WaitedMinutes}m",
            canServe: row.OperatorId is not null && !sectionIsServing,
            canReorder: true);

        await _popupService.ShowSheetAsync(sheet);
        var action = await sheet.Completion;

        switch (action)
        {
            case EntryAction.ServeNow:
                await ServeAsync(row);
                break;

            case EntryAction.MoveToAnotherOperator:
                await MoveToAnotherOperatorAsync(row);
                break;

            case EntryAction.MoveToEndOfQueue:
                await MoveToEndAsync(row);
                break;

            case EntryAction.ChangeService:
                await ChangeServiceAsync(row);
                break;

            case EntryAction.MarkNoShow:
                await ConfirmNoShowAsync(row.EntryId, row.CustomerName);
                break;

            case EntryAction.RemoveFromQueue:
                await ConfirmRemoveAsync(row.EntryId, row.CustomerName);
                break;
        }
    }

    // Offers the shared pool as a destination when the entry isn't already in it: sending one back
    // to operator_id = null is a real move, not an accident, so it has to be offered.
    private async Task MoveToAnotherOperatorAsync(QueueRowItem row)
    {
        var sheet = new AssignSheet(
            _popupService,
            row.CustomerName,
            row.Initials,
            $"{row.ServiceName} · waiting {row.WaitedMinutes}m",
            "MOVE TO",
            showNoShow: false,
            BuildAssignTargets(row.OperatorId, includePoolOption: row.OperatorId is not null));

        await _popupService.ShowSheetAsync(sheet);
        var result = await sheet.Completion;

        if (result.Assigned)
            await AssignEntryAsync(row, result.OperatorId);
    }

    private async Task MoveToEndAsync(QueueRowItem row)
    {
        row.IsBusy = true;
        try
        {
            ApplyLocally(row.EntryId, e => e.JoinedAt = DateTime.UtcNow);
            await _queueService.MoveEntryToEndAsync(row.EntryId);
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

    private async Task ChangeServiceAsync(QueueRowItem row)
    {
        var sheet = new ChangeServiceSheet(
            _popupService,
            $"Change service for {row.CustomerName}",
            BuildServiceRows(row.ServiceId));

        await _popupService.ShowSheetAsync(sheet);
        var serviceId = await sheet.Completion;

        if (serviceId is not { } chosen || chosen == row.ServiceId)
            return;

        try
        {
            ApplyLocally(row.EntryId, e => e.ServiceId = chosen);
            await _queueService.ChangeEntryServiceAsync(row.EntryId, chosen);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // Both destructive actions confirm, and both are reachable only from inside a sheet. Neither
    // has an undo: a mis-tap ejects someone who has physically stood there for fourteen minutes.
    private async Task ConfirmNoShowAsync(Guid entryId, string customerName)
    {
        var confirmed = await _popupService.ShowConfirmAsync("No-show", $"Mark {customerName} as a no-show?");
        if (!confirmed)
            return;

        try
        {
            ApplyLocally(entryId, e => e.Status = "no_show");
            await _queueService.NoShowAsync(entryId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    private async Task ConfirmRemoveAsync(Guid entryId, string customerName)
    {
        var confirmed = await _popupService.ShowConfirmAsync(
            "Remove from queue", $"Take {customerName} out of the queue?", "Remove", "Keep");
        if (!confirmed)
            return;

        try
        {
            ApplyLocally(entryId, e => e.Status = "cancelled");
            await _queueService.CancelEntryAsync(entryId);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    // One tappable line rather than an always-open input with its own Save button — the note is an
    // occasional action and shouldn't spend a third of the serving card on being available.
    [RelayCommand]
    private async Task EditNoteAsync(ServingCardItem? card)
    {
        if (card is null)
            return;

        var current = card.HasNote ? card.NoteText : string.Empty;
        var note = await _popupService.ShowPromptAsync(
            "Note", $"What's happening with {card.CustomerName}?", current,
            placeholder: "Colour treatment — running long");

        if (note is null)
            return;

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        card.HasNote = trimmed is not null;
        card.NoteText = trimmed ?? "Add a note";

        try
        {
            ApplyLocally(card.EntryId, e => e.ProgressStatus = trimmed);
            await _queueService.SetQueueProgressAsync(card.EntryId, trimmed);
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    [RelayCommand]
    private async Task AddWalkInAsync(BoardSection? section)
    {
        if (section is null)
            return;

        if (_services.Count == 0)
        {
            await _popupService.ShowAlertAsync(
                "No services yet", "Add a service under Settings before adding a walk-in.");
            return;
        }

        var summary = _summary.FirstOrDefault(r => r.OperatorId == section.OperatorId);
        var ahead = section.Waiting.Count + (section.HasServing ? 1 : 0);

        var sheet = new AddWalkInSheet(
            _popupService,
            $"Add to {section.Name}'s queue",
            section.OperatorId,
            ahead,
            summary?.NewJoinWaitMinutes ?? 0,
            BuildServiceRows(null));

        await _popupService.ShowSheetAsync(sheet);
        var request = await sheet.Completion;

        if (request is null)
            return;

        try
        {
            await _queueService.AddWalkInAsync(_businessId, request.OperatorId, request.Name, request.ServiceId);
            await LoadQueueAsync();
        }
        catch (Exception ex)
        {
            await ReloadAfterFailureAsync(ex);
        }
    }

    [RelayCommand]
    private async Task ToggleShiftAsync(BoardSection? section)
    {
        if (section is null || section.IsTogglingShift)
            return;

        section.IsTogglingShift = true;
        try
        {
            await _operatorService.SetOperatorAvailableAsync(section.OperatorId, !section.IsOnShift);

            // operators isn't on the realtime subscription — that stream is queue_entries — so a
            // shift change has to pull the board rather than wait to be told.
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

    // Optimistic write against the board's own copy of the stream, then an immediate rebuild, so
    // the screen moves on the tap. The realtime event that follows the real call re-reads and
    // overwrites this — including when the call failed and the optimism was wrong.
    private void ApplyLocally(Guid entryId, Action<QueueEntryResponse> mutate)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
            return;

        mutate(entry);
        _entries = _entries.Where(e => e.Status is "waiting" or "serving").ToList();
        Rebuild();
    }

    private async Task ReloadAfterFailureAsync(Exception ex)
    {
        await LoadQueueAsync();
        await HandleExceptionAsync(ex);
    }

    private List<AssignTargetItem> BuildAssignTargets(Guid? excludeOperatorId, bool includePoolOption)
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
                Initials = InitialsOf(op.DisplayName),
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

        // Soonest first — this sheet is the one place on the board where ordering by availability
        // is the point, because the question it asks is who can take the customer first.
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

    private List<ServiceChoiceRow> BuildServiceRows(Guid? selectedServiceId)
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

    private static string StatusTextFor(bool onShift, bool isServing, int waitingCount) => (onShift, isServing, waitingCount) switch
    {
        (false, _, _) => "Off shift",
        (true, true, 0) => "Serving · 0 waiting",
        (true, true, 1) => "Serving · 1 waiting",
        (true, true, var n) => $"Serving · {n} waiting",
        (true, false, 0) => "Free · nobody waiting",
        (true, false, 1) => "1 waiting",
        (true, false, var n) => $"{n} waiting",
    };

    // A walk-in with no name stays null in the database; the fallback lives here, at display time,
    // so a name that was given is the name that shows.
    private static string DisplayNameOf(QueueEntryResponse entry) =>
        string.IsNullOrWhiteSpace(entry.CustomerName) ? "Walk-in" : entry.CustomerName!;

    private string ServiceNameOf(Guid? serviceId) =>
        _services.FirstOrDefault(s => s.Id == serviceId)?.Name ?? string.Empty;

    private static int MinutesSince(DateTime timestamp) =>
        (int)Math.Max(0, (DateTime.UtcNow - BoardConstants.AsUtc(timestamp)).TotalMinutes);

    private static string InitialsOf(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    // Base HandleExceptionAsync only logs — this screen already has a popup service on hand, so use
    // it to surface real failures, most notably start_serving's "all resources are currently busy"
    // on a pooled business at capacity. That's a normal operational state for staff, not a fault.
    protected override Task HandleExceptionAsync(Exception exception)
    {
        return _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
    }
}
