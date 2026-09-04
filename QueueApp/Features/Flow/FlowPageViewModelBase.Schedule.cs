using QueueApp.Features.Flow.Constants;
using QueueApp.Features.Flow.Helpers;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow;

public abstract partial class FlowPageViewModelBase
{
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
}
