using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BookingAgenda.Sheets;

public sealed class BlockPresetOption : ObservableObject
{
    public required string Label { get; init; }
    public required Func<DateTime, (DateTimeOffset Start, DateTimeOffset End)> Resolve { get; init; }
    public bool IsSelected { get; set; }
}

// Blocking time doesn't cancel anything. Without the warning naming who is already booked inside
// the range, an operator will assume the app handled it and two people will arrive to a closed bay
// (spec §8).
public partial class BlockTimeSheetViewModel : ObservableObject
{
    private readonly DateTime _day;

    public BlockTimeSheetViewModel(
        DateTime day,
        IReadOnlyList<OperatorResponse> operators,
        CategoryLabelSet labels,
        IReadOnlyList<AgendaBookingResponse> knownBookings)
    {
        _day = day;
        _knownBookings = knownBookings;
        DayText = day.ToString("ddd d MMM");
        ResourceLabel = $"WHICH {labels.SectionTitle.ToUpperInvariant()}";

        foreach (var op in operators)
            Resources.Add(new BayFilterOption { OperatorId = op.Id, Label = op.DisplayName, IsSelected = true });

        Presets.Add(new BlockPresetOption
        {
            Label = "Next hour",
            Resolve = _ =>
            {
                var from = LocalTime.ToLocal(DateTimeOffset.UtcNow);
                return (from, from.AddHours(1));
            },
        });
        Presets.Add(new BlockPresetOption
        {
            Label = "Rest of today",
            Resolve = d =>
            {
                var from = LocalTime.ToLocal(DateTimeOffset.UtcNow);
                return (from, Midnight(d.AddDays(1)));
            },
        });
        Presets.Add(new BlockPresetOption
        {
            Label = "Tomorrow",
            Resolve = d => (Midnight(d.AddDays(1)), Midnight(d.AddDays(2))),
        });
        Presets.Add(new BlockPresetOption
        {
            Label = "Whole week",
            Resolve = d => (Midnight(d), Midnight(d.AddDays(7))),
        });

        // Straight to the backing fields: the setters kick off the warning recalculation, and there
        // is nothing to warn about until the page has wired up its range loader.
        _fromDate = day.Date;
        _untilDate = day.Date;
        _fromTime = new TimeSpan(9, 0, 0);
        _untilTime = new TimeSpan(17, 0, 0);
    }

    private IReadOnlyList<AgendaBookingResponse> _knownBookings;

    public string DayText { get; }
    public string ResourceLabel { get; }

    public ObservableCollection<BlockPresetOption> Presets { get; } = new();
    public ObservableCollection<BayFilterOption> Resources { get; } = new();

    // Written out by hand rather than left as auto-properties: every edit to the range has to
    // re-run the affected-bookings warning, and a stale warning here is the failure mode this whole
    // sheet exists to prevent.
    private DateTime _fromDate;
    private TimeSpan _fromTime;
    private DateTime _untilDate;
    private TimeSpan _untilTime;

    public DateTime FromDate
    {
        get => _fromDate;
        set { if (SetProperty(ref _fromDate, value)) _ = RecalculateAsync(); }
    }

    public TimeSpan FromTime
    {
        get => _fromTime;
        set { if (SetProperty(ref _fromTime, value)) _ = RecalculateAsync(); }
    }

    public DateTime UntilDate
    {
        get => _untilDate;
        set { if (SetProperty(ref _untilDate, value)) _ = RecalculateAsync(); }
    }

    public TimeSpan UntilTime
    {
        get => _untilTime;
        set { if (SetProperty(ref _untilTime, value)) _ = RecalculateAsync(); }
    }

    public string Reason { get; set; } = "";

    public string WarningText { get; set; } = "";
    public bool HasWarning => WarningText.Length > 0;

    public bool IsSaving { get; set; }
    public bool CanSubmit => !IsSaving && Resources.Any(r => r.IsSelected) && Until > From;

    private DateTimeOffset From => Sast(FromDate, FromTime);
    private DateTimeOffset Until => Sast(UntilDate, UntilTime);

    // A DatePicker hands back DateTimeKind.Local, and DateTimeOffset refuses a Local value whose
    // offset isn't the device's own — which is the whole point of pinning everything to +02:00.
    private static DateTimeOffset Sast(DateTime date, TimeSpan time) =>
        new(DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified), LocalTime.Offset);

    private static DateTimeOffset Midnight(DateTime date) => Sast(date, TimeSpan.Zero);

    // Supplied by the page so the warning can look past the day on screen — "Whole week" blocks six
    // days the agenda never loaded.
    public Func<DateTimeOffset, DateTimeOffset, Task<List<AgendaBookingResponse>>>? LoadBookingsInRange { get; init; }

    public Func<List<CreateAvailabilityBlockRequest>, Task>? OnBlock { get; init; }
    public Func<Task>? OnDismiss { get; init; }

    [RelayCommand]
    private async Task SelectPresetAsync(BlockPresetOption preset)
    {
        foreach (var option in Presets)
            option.IsSelected = option == preset;

        var (start, end) = preset.Resolve(_day);

        // Four fields, one recalculation — going through the setters would fire the range query
        // four times for a single tap.
        _fromDate = start.Date;
        _fromTime = start.TimeOfDay;
        _untilDate = end.Date;
        _untilTime = end.TimeOfDay;

        OnPropertyChanged(nameof(FromDate));
        OnPropertyChanged(nameof(FromTime));
        OnPropertyChanged(nameof(UntilDate));
        OnPropertyChanged(nameof(UntilTime));

        await RecalculateAsync();
    }

    [RelayCommand]
    private async Task ToggleResourceAsync(BayFilterOption option)
    {
        option.IsSelected = !option.IsSelected;
        OnPropertyChanged(nameof(CanSubmit));
        await RecalculateAsync();
    }

    [RelayCommand]
    private Task DismissAsync() => OnDismiss?.Invoke() ?? Task.CompletedTask;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit || OnBlock is null)
            return;

        IsSaving = true;
        try
        {
            var reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim();

            // availability_blocks is operator-scoped, so blocking two bays is two rows.
            var requests = Resources
                .Where(r => r.IsSelected && r.OperatorId is not null)
                .Select(r => new CreateAvailabilityBlockRequest
                {
                    OperatorId = r.OperatorId!.Value,
                    StartsAt = From,
                    EndsAt = Until,
                    Reason = reason,
                })
                .ToList();

            await OnBlock(requests);
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task RecalculateAsync()
    {
        OnPropertyChanged(nameof(CanSubmit));

        if (Until <= From)
        {
            WarningText = "";
            return;
        }

        var selected = Resources.Where(r => r.IsSelected && r.OperatorId is not null)
            .Select(r => r.OperatorId!.Value)
            .ToHashSet();

        if (selected.Count == 0)
        {
            WarningText = "";
            return;
        }

        var candidates = _knownBookings;
        if (LoadBookingsInRange is not null)
        {
            try
            {
                candidates = await LoadBookingsInRange(From, Until);
                _knownBookings = candidates;
            }
            catch (Exception)
            {
                // Fall back to the day already on screen rather than showing no warning at all —
                // a partial list of stranded customers still beats silence.
            }
        }

        var affected = candidates
            .Where(b => BookingStatuses.OccupiesTheDiary(b.Status))
            .Where(b => b.OperatorId is not null && selected.Contains(b.OperatorId.Value))
            .Where(b => b.StartsAt < Until && b.EndsAt > From)
            .OrderBy(b => b.StartsAt)
            .ToList();

        WarningText = Describe(affected);
    }

    private static string Describe(IReadOnlyList<AgendaBookingResponse> affected)
    {
        if (affected.Count == 0)
            return "";

        var first = affected[0];
        var lead = $"{first.CustomerName} is booked {first.TimeRangeDisplay}";

        var rest = affected.Count switch
        {
            1 => "",
            2 => $", and {affected[1].CustomerName} at {affected[1].LocalStart:HH:mm}",
            _ => $", and {affected.Count - 1} others",
        };

        return $"{lead} inside this range{rest}. Blocking won't cancel them — you'll need to call.";
    }
}
