using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.BookingAgenda.Models;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.BookingAgenda.Sheets;

public partial class BlockTimeSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<BlockTimeResult> _completion = new();

    private IReadOnlyList<AgendaBookingResponse> _knownBookings;

    private DateTime _fromDate;
    private TimeSpan _fromTime;
    private DateTime _untilDate;
    private TimeSpan _untilTime;

    public string DayText { get; }
    public string ResourceLabel { get; }

    public ObservableCollection<BayFilterOption> Resources { get; } = new();

    public ICommand ToggleResourceCommand { get; }

    public string Reason { get; set; } = string.Empty;
    public string WarningText { get; private set; } = string.Empty;
    public bool HasWarning => WarningText.Length > 0;

    public Func<DateTimeOffset, DateTimeOffset, Task<List<AgendaBookingResponse>>>? LoadBookingsInRange { get; set; }

    public Task<BlockTimeResult> Completion => _completion.Task;

    public DateTimeOffset From => AgendaConstants.Sast(FromDate, FromTime);
    public DateTimeOffset Until => AgendaConstants.Sast(UntilDate, UntilTime);

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value) return;
            _fromDate = value;
            OnPropertyChanged();
            _ = RecalculateAsync();
        }
    }

    public TimeSpan FromTime
    {
        get => _fromTime;
        set
        {
            if (_fromTime == value) return;
            _fromTime = value;
            OnPropertyChanged();
            _ = RecalculateAsync();
        }
    }

    public DateTime UntilDate
    {
        get => _untilDate;
        set
        {
            if (_untilDate == value) return;
            _untilDate = value;
            OnPropertyChanged();
            _ = RecalculateAsync();
        }
    }

    public TimeSpan UntilTime
    {
        get => _untilTime;
        set
        {
            if (_untilTime == value) return;
            _untilTime = value;
            OnPropertyChanged();
            _ = RecalculateAsync();
        }
    }

    public BlockTimeSheet()
        : this(null!, DateTime.Today, [], CategoryLabels.Resolve(null), [])
    {
    }

    public BlockTimeSheet(
        IQueuePopupService popups,
        DateTime day,
        IReadOnlyList<OperatorResponse> operators,
        CategoryLabelSet labels,
        IReadOnlyList<AgendaBookingResponse> knownBookings)
    {
        _popups = popups;
        _knownBookings = knownBookings;

        DayText = day.ToString("ddd d MMM");
        ResourceLabel = $"WHICH {labels.SectionTitle.ToUpperInvariant()}";

        foreach (var resource in operators)
            Resources.Add(new BayFilterOption
            {
                OperatorId = resource.Id,
                Label = resource.DisplayName,
                IsSelected = true,
            });

        _fromDate = day.Date;
        _untilDate = day.Date;
        _fromTime = new TimeSpan(AgendaConstants.FallbackOpenHour, 0, 0);
        _untilTime = new TimeSpan(AgendaConstants.FallbackCloseHour, 0, 0);

        ToggleResourceCommand = new RelayCommand<BayFilterOption>(ToggleResource);

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(new BlockTimeResult(false));
    }

    public void ToggleResource(BayFilterOption? resource)
    {
        try
        {
            if (resource is null)
                return;

            resource.IsSelected = !resource.IsSelected;
            _ = RecalculateAsync();
        }
        catch (Exception)
        {
        }
    }

    public async Task RecalculateAsync()
    {
        try
        {
            var selected = SelectedOperatorIds();

            if (Until <= From || selected.Count == 0)
            {
                SetWarning(string.Empty);
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
                }
            }

            var affected = candidates
                .Where(b => BookingStatuses.OccupiesTheDiary(b.Status))
                .Where(b => b.OperatorId is not null && selected.Contains(b.OperatorId.Value))
                .Where(b => b.StartsAt < Until && b.EndsAt > From)
                .OrderBy(b => b.StartsAt)
                .ToList();

            SetWarning(Describe(affected));
        }
        catch (Exception)
        {
            SetWarning(string.Empty);
        }
    }

    public List<Guid> SelectedOperatorIds() =>
        Resources
            .Where(r => r.IsSelected && r.OperatorId is not null)
            .Select(r => r.OperatorId!.Value)
            .ToList();

    public static string Describe(IReadOnlyList<AgendaBookingResponse> affected)
    {
        if (affected.Count == 0)
            return string.Empty;

        var first = affected[0];
        var lead = $"{first.CustomerName} is booked {first.TimeRangeDisplay}";

        var rest = affected.Count switch
        {
            1 => string.Empty,
            2 => $", and {affected[1].CustomerName} at {affected[1].LocalStart:HH:mm}",
            _ => $", and {affected.Count - 1} others",
        };

        return $"{lead} inside this range{rest}. Blocking won't cancel them — you'll need to call.";
    }

    private void SetWarning(string text)
    {
        if (WarningText == text)
            return;

        WarningText = text;
        OnPropertyChanged(nameof(WarningText));
        OnPropertyChanged(nameof(HasWarning));
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        try
        {
            var selected = SelectedOperatorIds();

            if (selected.Count == 0 || Until <= From)
            {
                await _popups.ShowAlertAsync(
                    "Check the range",
                    "Pick at least one resource, and an end time after the start.");
                return;
            }

            Close(new BlockTimeResult(
                true,
                selected,
                From,
                Until,
                string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim()));
        }
        catch (Exception)
        {
            Close(new BlockTimeResult(false));
        }
    }

    private void Close(BlockTimeResult result)
    {
        _completion.TrySetResult(result);
        _ = _popups.HideSheetAsync(this);
    }
}
