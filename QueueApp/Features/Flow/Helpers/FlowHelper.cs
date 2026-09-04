using QueueApp.Features.Flow.Constants;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow.Helpers;

public static class FlowHelper
{
    public static List<SlotChoiceItem> SlotsInPeriod(
        IEnumerable<SlotChoiceItem> items,
        int fromHour,
        int toHour) =>
        items
            .Where(i =>
            {
                var hour = LocalTime.ToLocal(i.Slot.SlotStart).Hour;
                return hour >= fromHour && hour < toHour;
            })
            .ToList();

    public static string SlotRangeText(SlotChoiceItem? slot)
    {
        if (slot is null)
            return FlowConstants.PickTimePrompt;

        var start = LocalTime.ToLocal(slot.Slot.SlotStart);
        var end = LocalTime.ToLocal(slot.Slot.SlotEnd);
        return $"{start:ddd d} · {start:HH:mm} – {end:HH:mm}";
    }

    public static List<DayChoiceItem> BuildDayStrip(int length)
    {
        var days = new List<DayChoiceItem>(length);

        for (var i = 0; i < length; i++)
        {
            var date = LocalTime.Now.Date.AddDays(i);
            days.Add(new DayChoiceItem
            {
                Date = date,
                DayOfWeekText = date.ToString("ddd").ToUpperInvariant(),
                DayNumberText = date.Day.ToString(),
            });
        }

        return days;
    }
}
