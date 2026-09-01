using QueueApp.Features.Flow.Visit.Models;
using QueueApp.Services.Popup;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.Flow.Visit.Sheets;

public partial class VisitOptionsSheet : BottomSheetPage
{
    public string BusinessName { get; }
    public string SummaryText { get; }
    public string LeavingCostText { get; }
    public bool HasPhone { get; }
    public bool CanShare { get; }
    public bool CanAddToCalendar { get; }
    public bool CanGoAgain { get; }
    public bool CanLeaveQueue { get; }
    public bool CanCancelBooking { get; }
    public bool HasDestructiveAction => CanLeaveQueue || CanCancelBooking;

    public Task<VisitOptionResult> Completion => _completion.Task;

    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<VisitOptionResult> _completion = new();

    public VisitOptionsSheet()
        : this(null!, null!, false, string.Empty)
    {
    }

    public VisitOptionsSheet(IQueuePopupService popups, VisitRecord record, bool hasPhone, string leavingCostText)
    {
        _popups = popups;

        BusinessName = record?.BusinessName ?? string.Empty;
        SummaryText = BuildSummary(record);
        LeavingCostText = leavingCostText;
        HasPhone = hasPhone;

        CanShare = record?.IsLive == true;
        CanAddToCalendar = record is { IsLive: true, IsBooking: true, SlotStart: not null };
        CanGoAgain = record is not null && !record.IsLive;
        CanLeaveQueue = record is { IsLive: true, IsQueue: true };
        CanCancelBooking = record is { IsLive: true, IsBooking: true };

        InitializeComponent();
    }

    public static string BuildSummary(VisitRecord? record)
    {
        if (record is null)
            return string.Empty;

        if (record.IsBooking && record.SlotStart is { } slot)
            return $"{record.ServiceName} · {LocalTime.ToLocal(slot):ddd d MMM HH:mm}";

        return record.JoinedAt is { } joined
            ? $"{record.ServiceName} · joined {LocalTime.ToLocal(joined):HH:mm}"
            : record.ServiceName;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(new VisitOptionResult(VisitOption.Dismissed));
    }

    private void OnCallClicked(object? sender, EventArgs e) => Close(VisitOption.Call);

    private void OnDirectionsClicked(object? sender, EventArgs e) => Close(VisitOption.Directions);

    private void OnShareClicked(object? sender, EventArgs e) => Close(VisitOption.Share);

    private void OnCalendarClicked(object? sender, EventArgs e) => Close(VisitOption.AddToCalendar);

    private void OnGoAgainClicked(object? sender, EventArgs e) => Close(VisitOption.GoAgain);

    private void OnLeaveClicked(object? sender, EventArgs e) => Close(VisitOption.LeaveQueue);

    private void OnCancelBookingClicked(object? sender, EventArgs e) => Close(VisitOption.CancelBooking);

    private void Close(VisitOption option)
    {
        _completion.TrySetResult(new VisitOptionResult(option));
        _ = _popups.HideSheetAsync(this);
    }
}
