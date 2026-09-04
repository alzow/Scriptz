using QueueApp.Features.Flow.Constants;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;

namespace QueueApp.Features.Flow.Helpers;

public sealed record FlowScheduleKey(Guid BusinessId, Guid? OperatorId, Guid ServiceId, bool IsAnyAvailable);

public sealed class FlowScheduleLoader
{
    private readonly Dictionary<(FlowScheduleKey Key, DateTime Date), List<SlotResponse>> _slotCache = new();
    private readonly Dictionary<FlowScheduleKey, Dictionary<DateTime, int>> _dayCountCache = new();
    private CancellationTokenSource? _slotDebounce;

    private readonly IBookingService _bookingService;

    public FlowScheduleLoader(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public bool TryGetDayCounts(FlowScheduleKey key, out IReadOnlyDictionary<DateTime, int> counts)
    {
        if (_dayCountCache.TryGetValue(key, out var cached))
        {
            counts = cached;
            return true;
        }

        counts = new Dictionary<DateTime, int>();
        return false;
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> LoadDayCountsAsync(
        FlowScheduleKey key,
        IReadOnlyList<DateTime> dates)
    {
        if (_dayCountCache.TryGetValue(key, out var cached))
            return cached;

        var results = await Task.WhenAll(dates.Select(async date =>
        {
            var slots = await FetchSlotsAsync(key, date);
            return (date, slots);
        }));

        // Counting a day means fetching its slots, so the time step is served from what the day
        // step already paid for instead of repeating the same RPC one chip later.
        foreach (var (date, slots) in results)
            _slotCache[(key, date)] = slots;

        var counts = results.ToDictionary(r => r.date, r => r.slots.Count);
        _dayCountCache[key] = counts;

        return counts;
    }

    public bool TryGetSlots(FlowScheduleKey key, DateTime date, out IReadOnlyList<SlotResponse> slots)
    {
        if (_slotCache.TryGetValue((key, date), out var cached))
        {
            slots = cached;
            return true;
        }

        slots = Array.Empty<SlotResponse>();
        return false;
    }

    // Returns null when a newer day selection superseded this one — flicking along the day strip
    // must not fire an RPC per chip, and the caller must not paint a result it no longer wants.
    public async Task<IReadOnlyList<SlotResponse>?> LoadSlotsAsync(FlowScheduleKey key, DateTime date)
    {
        if (_slotCache.TryGetValue((key, date), out var cached))
            return cached;

        _slotDebounce?.Cancel();
        _slotDebounce = new CancellationTokenSource();
        var token = _slotDebounce.Token;

        try
        {
            await Task.Delay(FlowConstants.SlotDebounceMilliseconds, token);

            var slots = await FetchSlotsAsync(key, date);

            if (token.IsCancellationRequested)
                return null;

            _slotCache[(key, date)] = slots;
            return slots;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    public void InvalidateSlots()
    {
        _slotCache.Clear();
    }

    public void CancelPendingSlotLoad()
    {
        _slotDebounce?.Cancel();
        _slotDebounce?.Dispose();
        _slotDebounce = null;
    }

    private Task<List<SlotResponse>> FetchSlotsAsync(FlowScheduleKey key, DateTime date) =>
        key.IsAnyAvailable || key.OperatorId is null
            ? _bookingService.GetAvailableSlotsAnyAsync(key.BusinessId, key.ServiceId, date)
            : _bookingService.GetAvailableSlotsAsync(key.OperatorId.Value, key.ServiceId, date);
}
