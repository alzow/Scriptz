using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel.Communication;
using QueueApp.Features.BookingAgenda.Models;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.BookingAgenda.Sheets;

public partial class BookingActionsSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<BookingActionResult> _completion = new();
    private readonly string? _phone;
    private readonly bool _isInProgress;

    public string CustomerName { get; }
    public string Initials { get; }
    public string WhenText { get; }
    public string ServiceName { get; }
    public string PriceText { get; }
    public string BookedAgoText { get; }
    public string PrimaryActionText { get; }
    public bool HasPrimary { get; }
    public bool HasPhone { get; }
    public bool CanCancel { get; }
    public bool CanMarkNoShow { get; }
    public string ProgressStatus { get; set; }

    public ObservableCollection<MoveTargetOption> MoveTargets { get; } = new();
    public bool HasMoveTargets => MoveTargets.Count > 0;

    public Task<BookingActionResult> Completion => _completion.Task;

    public BookingActionsSheet()
        : this(null!, null!, Array.Empty<OperatorResponse>())
    {
    }

    public BookingActionsSheet(
        IQueuePopupService popups,
        AgendaBookingResponse booking,
        IReadOnlyList<OperatorResponse> otherResources)
    {
        _popups = popups;

        CustomerName = booking?.CustomerName ?? string.Empty;
        Initials = booking?.Initials ?? string.Empty;
        WhenText = BuildWhenText(booking);
        ServiceName = booking?.ServiceName ?? string.Empty;
        PriceText = booking is not null && booking.PriceText.Length > 0 ? booking.PriceText : "No price set";
        BookedAgoText = DescribeBooked(booking?.CreatedAt ?? default);
        HasPrimary = booking is not null && (booking.CanStart || booking.IsInProgress);
        PrimaryActionText = booking?.IsInProgress == true
            ? "They're finished — done"
            : "They've arrived — start";
        CanCancel = booking?.CanCancel ?? false;
        CanMarkNoShow = booking is not null && !booking.IsFinished;
        ProgressStatus = booking?.ProgressStatus ?? string.Empty;

        _isInProgress = booking?.IsInProgress ?? false;
        _phone = booking?.CustomerPhone;
        HasPhone = booking?.HasPhone ?? false;

        foreach (var resource in otherResources)
            MoveTargets.Add(new MoveTargetOption { Operator = resource });

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(new BookingActionResult(BookingAction.Dismissed));
    }

    public static string BuildWhenText(AgendaBookingResponse? booking)
    {
        if (booking is null)
            return string.Empty;

        return booking.Operator is null
            ? booking.DayAndRangeDisplay
            : $"{booking.DayAndRangeDisplay} · {booking.Operator.DisplayName}";
    }

    public static string DescribeBooked(DateTimeOffset createdAt)
    {
        if (createdAt == default)
            return AgendaConstants.EmDash;

        var age = DateTimeOffset.UtcNow - createdAt;

        if (age.TotalMinutes < 60)
            return $"{Math.Max(1, (int)age.TotalMinutes)} min ago";

        if (age.TotalHours < 24)
            return (int)age.TotalHours == 1 ? "1 hour ago" : $"{(int)age.TotalHours} hours ago";

        var days = (int)age.TotalDays;
        return days == 1 ? "Yesterday" : $"{days} days ago";
    }

    private void OnPrimaryClicked(object? sender, EventArgs e) =>
        Close(new BookingActionResult(_isInProgress ? BookingAction.Complete : BookingAction.Start));

    private void OnMoveTimeClicked(object? sender, EventArgs e) =>
        Close(new BookingActionResult(BookingAction.MoveToAnotherTime));

    private void OnMoveResourceClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: MoveTargetOption target })
            Close(new BookingActionResult(BookingAction.MoveToResource, target.Operator.Id));
    }

    private void OnNoShowClicked(object? sender, EventArgs e) =>
        Close(new BookingActionResult(BookingAction.MarkNoShow));

    private void OnCancelClicked(object? sender, EventArgs e) =>
        Close(new BookingActionResult(BookingAction.Cancel));

    private void OnSaveProgressClicked(object? sender, EventArgs e) =>
        Close(new BookingActionResult(
            BookingAction.SaveProgress,
            ProgressStatus: string.IsNullOrWhiteSpace(ProgressStatus) ? null : ProgressStatus.Trim()));

    private void OnCallClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_phone))
                PhoneDialer.Default.Open(_phone);
        }
        catch (Exception)
        {
        }
    }

    private void Close(BookingActionResult result)
    {
        _completion.TrySetResult(result);
        _ = _popups.HideSheetAsync(this);
    }
}
