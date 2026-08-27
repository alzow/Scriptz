using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Features.BusinessDetail.Flow;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed class BlockPresetOption : ObservableObject
{
    public required string Label { get; init; }
    public required Func<DateTime, (DateTimeOffset Start, DateTimeOffset End)> Resolve { get; init; }
    public bool IsSelected { get; set; }

    public static List<BlockPresetOption> BuildAll() =>
    [
        new()
        {
            Label = "Next hour",
            Resolve = _ =>
            {
                var from = LocalTime.ToLocal(DateTimeOffset.UtcNow);
                return (from, from.AddHours(1));
            },
        },
        new()
        {
            Label = "Rest of today",
            Resolve = day =>
            {
                var from = LocalTime.ToLocal(DateTimeOffset.UtcNow);
                return (from, Midnight(day.AddDays(1)));
            },
        },
        new()
        {
            Label = "Tomorrow",
            Resolve = day => (Midnight(day.AddDays(1)), Midnight(day.AddDays(2))),
        },
        new()
        {
            Label = "Whole week",
            Resolve = day => (Midnight(day), Midnight(day.AddDays(7))),
        },
    ];

    public static DateTimeOffset Midnight(DateTime date) => Sast(date, TimeSpan.Zero);

    public static DateTimeOffset Sast(DateTime date, TimeSpan time) =>
        new(DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified), LocalTime.Offset);
}
