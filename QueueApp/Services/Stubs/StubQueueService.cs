using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real QueueService in DEBUG builds.
public class StubQueueService : IQueueService
{
    private readonly List<QueueEntryResponse> _entries = new();

    public Task<List<QueueEntryResponse>> GetActiveEntriesAsync(Guid businessId)
        => Task.FromResult(_entries
            .Where(e => e.BusinessId == businessId && e.Status is "waiting" or "serving" or QueueEntryStatuses.AwaitingCollection)
            .OrderBy(e => e.JoinedAt)
            .ToList());

    // Goes through join_queue for real, so a counter add with nobody named resolves the same way
    // a customer's join does.
    public Task AddWalkInAsync(Guid businessId, Guid? operatorId, string? name, Guid serviceId)
    {
        _entries.Add(new QueueEntryResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorId = operatorId ?? PickFastestOperator(businessId),
            ServiceId = serviceId,
            CustomerName = name,
            Status = "waiting",
            JoinedAt = DateTime.UtcNow,
        });
        return Task.CompletedTask;
    }

    public Task StartServingAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null || entry.Status != "waiting")
            throw new InvalidOperationException("entry not in waiting state");

        var alreadyServing = _entries.Any(e =>
            e.BusinessId == entry.BusinessId && e.OperatorId == entry.OperatorId && e.Status == "serving");
        if (alreadyServing)
            throw new InvalidOperationException("this operator is already serving another customer");

        entry.Status = "serving";
        entry.ServingAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task CompleteAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
        {
            entry.Status = QueueEntryStatuses.Done;
            entry.DoneAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task MarkAwaitingCollectionAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
        {
            entry.Status = QueueEntryStatuses.AwaitingCollection;
            entry.AwaitingCollectionAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task MarkCollectedAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
        {
            var now = DateTime.UtcNow;
            entry.Status = QueueEntryStatuses.Done;
            entry.DoneAt = now;
            entry.CollectedAt = now;
        }
        return Task.CompletedTask;
    }

    public Task NoShowAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.Status = "no_show";
        return Task.CompletedTask;
    }

    // Shaped like business_queue_summary, which is a row per active operator — never a row for the
    // unassigned ones. Grouping by operator_id used to invent an "Any available" row that no real
    // response contains, which is exactly the row every "shortest wait" read would then have
    // believed.
    public Task<List<QueueSummaryRow>> GetQueueSummaryAsync(Guid businessId)
    {
        var live = _entries
            .Where(e => e.BusinessId == businessId && (e.Status == "waiting" || e.Status == "serving"))
            .ToList();

        var rows = StubOperatorService.Roster
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder)
            .Select(o => new QueueSummaryRow
            {
                OperatorId = o.Id,
                OperatorName = o.DisplayName,
                IsAvailable = o.IsAvailable,
                WaitingCount = live.Count(e => e.OperatorId == o.Id && e.Status == "waiting"),
                ServingCount = live.Count(e => e.OperatorId == o.Id && e.Status == "serving"),
                NewJoinWaitMinutes = live.Count(e => e.OperatorId == o.Id && e.Status == "waiting") * 10,
            })
            .ToList();
        return Task.FromResult(rows);
    }

    // Mirrors join_queue: a null operator means "pick for me", not "leave it in the pool", so a
    // stubbed run and a real one put the customer in the same chair.
    public Task<QueueEntryResponse> JoinQueueAsync(Guid businessId, Guid? operatorId, Guid customerId, string? customerName, Guid serviceId,
        Dictionary<string, IntakeAnswer>? intakeResponses = null)
    {
        var resolved = operatorId ?? PickFastestOperator(businessId);

        var entry = new QueueEntryResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            OperatorId = resolved,
            ServiceId = serviceId,
            CustomerId = customerId,
            CustomerName = customerName,
            Status = "waiting",
            JoinedAt = DateTime.UtcNow,
            Details = operatorId is null && resolved is not null
                ? new QueueEntryDetails { Assigned = AssignedValues.Auto }
                : null,
        };
        _entries.Add(entry);
        return Task.FromResult(entry);
    }

    private static string? NameOf(Guid? operatorId) => operatorId is { } id
        ? StubOperatorService.Roster.FirstOrDefault(o => o.Id == id)?.DisplayName
        : null;

    // An assigned entry is nth in its operator's line. An unassigned one has no line of its own,
    // so it counts everyone ahead of it in the shop — a private ranking would call it 1st.
    private static int PositionOf(QueueEntryResponse mine, List<QueueEntryResponse> shopWide) =>
        mine.OperatorId is null
            ? shopWide.Count(e => e.JoinedAt < mine.JoinedAt) + 1
            : shopWide.Where(e => e.OperatorId == mine.OperatorId).ToList().IndexOf(mine) + 1;

    // Null when the shop has nobody on shift — the one case that still lands unassigned. The
    // stub's wait is waiting × 10, so ordering on the count is ordering on the wait; sort_order
    // breaks the tie, as it does in the SQL.
    private Guid? PickFastestOperator(Guid businessId)
    {
        var waiting = _entries
            .Where(e => e.BusinessId == businessId && e.Status == "waiting")
            .ToList();

        return StubOperatorService.Roster
            .Where(o => o.IsActive && o.IsAvailable)
            .OrderBy(o => waiting.Count(e => e.OperatorId == o.Id))
            .ThenBy(o => o.SortOrder)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefault();
    }

    public Task<QueueEntryResponse> CancelEntryAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.Status = "cancelled";
        return Task.FromResult(entry ?? new QueueEntryResponse { Id = entryId, Status = "cancelled" });
    }

    public Task<MyQueueStatusResponse?> GetMyQueueStatusAsync(Guid businessId)
    {
        var waiting = _entries
            .Where(e => e.BusinessId == businessId && (e.Status == "waiting" || e.Status == "serving"))
            .OrderBy(e => e.JoinedAt)
            .ToList();

        var mine = waiting.LastOrDefault();
        if (mine is null)
            return Task.FromResult<MyQueueStatusResponse?>(null);

        // Ranked within the operator's own line, or against the whole shop when there is no
        // operator — the same split my_queue_status makes.
        var position = PositionOf(mine, waiting);

        return Task.FromResult<MyQueueStatusResponse?>(new MyQueueStatusResponse
        {
            EntryId = mine.Id,
            OperatorId = mine.OperatorId,
            OperatorName = NameOf(mine.OperatorId),
            Position = position,
            Status = mine.Status,
            JoinedAt = mine.JoinedAt,
        });
    }

    public Task<MyActiveQueueEntryResponse?> GetMyActiveEntryAsync()
    {
        var mine = _entries
            .Where(e => e.Status is "waiting" or "serving" or QueueEntryStatuses.AwaitingCollection)
            .OrderBy(e => e.JoinedAt)
            .LastOrDefault();

        if (mine is null)
            return Task.FromResult<MyActiveQueueEntryResponse?>(null);

        var position = PositionOf(mine, _entries
            .Where(e => e.BusinessId == mine.BusinessId && e.Status is "waiting" or "serving" or QueueEntryStatuses.AwaitingCollection)
            .OrderBy(e => e.JoinedAt)
            .ToList());

        return Task.FromResult<MyActiveQueueEntryResponse?>(new MyActiveQueueEntryResponse
        {
            EntryId = mine.Id,
            BusinessId = mine.BusinessId,
            BusinessName = "Nu-Look Barbers",
            BusinessLatitude = -26.3167,
            BusinessLongitude = 27.8500,
            OperatorId = mine.OperatorId,
            OperatorName = NameOf(mine.OperatorId),
            Position = position,
            Status = mine.Status,
            JoinedAt = mine.JoinedAt,
            // Null for an unassigned entry, matching queue_entry_wait_minutes on a row that
            // belongs to nobody's line: there is nothing to add up.
            WaitMinutes = mine.OperatorId is null ? null : position * 7,
            ProgressStatus = mine.ProgressStatus,
        });
    }

    public Task<decimal?> GetEntryWaitMinutesAsync(Guid entryId)
        => Task.FromResult<decimal?>(10);

    public Task<QueueEntryResponse> SetQueueProgressAsync(Guid entryId, string? status)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.ProgressStatus = status;
        return Task.FromResult(entry ?? new QueueEntryResponse { Id = entryId, ProgressStatus = status });
    }

    public Task<List<MyQueueEntryResponse>> GetMyEntriesAsync(Guid customerId)
        => Task.FromResult(new List<MyQueueEntryResponse>());

    public Task<MyQueueEntryResponse?> GetEntryAsync(Guid entryId)
        => Task.FromResult<MyQueueEntryResponse?>(null);

    public Task StampEntryCancelledByCustomerAsync(Guid entryId, QueueEntryDetails? existing)
        => Task.CompletedTask;

    public Task AssignEntryAsync(Guid entryId, Guid? operatorId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.OperatorId = operatorId;
        return Task.CompletedTask;
    }

    public Task MoveEntryToEndAsync(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.JoinedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task ChangeEntryServiceAsync(Guid entryId, Guid serviceId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.ServiceId = serviceId;
        return Task.CompletedTask;
    }

    public Task SetEntryNoteAsync(Guid entryId, string? note)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null) entry.ProgressStatus = note;
        return Task.CompletedTask;
    }

    public Task<List<QueueEntryResponse>> GetCompletedTodayAsync(Guid businessId)
        => Task.FromResult(_entries
            .Where(e => e.BusinessId == businessId && e.DoneAt >= DateTime.UtcNow.Date)
            .ToList());
}
