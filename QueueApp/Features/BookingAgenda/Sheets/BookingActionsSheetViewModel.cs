using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel.Communication;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BookingAgenda.Sheets;

public sealed class MoveTargetOption
{
    public required OperatorResponse Operator { get; init; }
    public string Label => $"Move to {Operator.DisplayName}";
}

// What you can do to one booking, read at arm's length by someone holding a spanner. The copy is
// deliberately plain — "They've arrived — start", "Didn't show up" — not "Check in" and "Mark as
// no-show" (spec §6).
public partial class BookingActionsSheetViewModel : ObservableObject
{
    public BookingActionsSheetViewModel(
        AgendaBookingResponse booking,
        IReadOnlyList<OperatorResponse> otherResources)
    {
        Booking = booking;
        ProgressStatus = booking.ProgressStatus ?? "";

        foreach (var op in otherResources)
            MoveTargets.Add(new MoveTargetOption { Operator = op });
    }

    public AgendaBookingResponse Booking { get; }

    public ObservableCollection<MoveTargetOption> MoveTargets { get; } = new();
    public bool HasMoveTargets => MoveTargets.Count > 0;

    public string Initials => Booking.Initials;
    public string CustomerName => Booking.CustomerName;

    public string WhenText => Booking.Operator is null
        ? Booking.DayAndRangeDisplay
        : $"{Booking.DayAndRangeDisplay} · {Booking.Operator.DisplayName}";

    public string ServiceName => Booking.ServiceName;
    public string PriceText => Booking.PriceText.Length > 0 ? Booking.PriceText : "No price set";
    public string BookedAgoText => DescribeBooked(Booking.CreatedAt);

    public bool HasPhone => Booking.HasPhone;

    // One filled green per region: the primary is whichever of start/finish the booking is actually
    // waiting for, never both.
    public bool HasPrimary => Booking.CanStart || Booking.IsInProgress;

    public string PrimaryActionText => Booking.IsInProgress
        ? "They're finished — done"
        : "They've arrived — start";

    public bool CanCancel => Booking.CanCancel;
    public bool CanMarkNoShow => !Booking.IsFinished;

    // Carried over from the agenda screen this replaced. The spec's sheet doesn't list it, but it
    // was already shipping and a customer watching their booking's status is the one who'd miss it.
    // Quiet, and it never blocks or closes anything — same treatment as the queue board's note.
    public string ProgressStatus { get; set; }
    public bool IsSavingProgress { get; set; }

    public Func<AgendaBookingResponse, string?, Task>? OnSaveProgress { get; init; }
    public Func<AgendaBookingResponse, Task>? OnStart { get; init; }
    public Func<AgendaBookingResponse, Task>? OnComplete { get; init; }
    public Func<AgendaBookingResponse, Task>? OnNoShow { get; init; }
    public Func<AgendaBookingResponse, Task>? OnCancel { get; init; }
    public Func<AgendaBookingResponse, OperatorResponse, Task>? OnMoveToResource { get; init; }
    public Func<AgendaBookingResponse, Task>? OnMoveToAnotherTime { get; init; }
    public Func<Task>? OnDismiss { get; init; }

    public bool IsBusy { get; set; }

    [RelayCommand]
    private Task PrimaryAsync() => GuardAsync(() => Booking.IsInProgress
        ? OnComplete?.Invoke(Booking) ?? Task.CompletedTask
        : OnStart?.Invoke(Booking) ?? Task.CompletedTask);

    [RelayCommand]
    private async Task SaveProgressAsync()
    {
        if (IsSavingProgress || OnSaveProgress is null)
            return;

        IsSavingProgress = true;
        try
        {
            var note = string.IsNullOrWhiteSpace(ProgressStatus) ? null : ProgressStatus.Trim();
            await OnSaveProgress(Booking, note);
            Booking.ProgressStatus = note;
        }
        finally
        {
            IsSavingProgress = false;
        }
    }

    [RelayCommand]
    private Task MoveTimeAsync() => GuardAsync(() => OnMoveToAnotherTime?.Invoke(Booking) ?? Task.CompletedTask);

    [RelayCommand]
    private Task MoveResourceAsync(MoveTargetOption target) =>
        GuardAsync(() => OnMoveToResource?.Invoke(Booking, target.Operator) ?? Task.CompletedTask);

    [RelayCommand]
    private Task NoShowAsync() => GuardAsync(() => OnNoShow?.Invoke(Booking) ?? Task.CompletedTask);

    [RelayCommand]
    private Task CancelBookingAsync() => GuardAsync(() => OnCancel?.Invoke(Booking) ?? Task.CompletedTask);

    [RelayCommand]
    private Task DismissAsync() => OnDismiss?.Invoke() ?? Task.CompletedTask;

    [RelayCommand]
    private async Task CallAsync()
    {
        if (!HasPhone)
            return;

        try
        {
            PhoneDialer.Default.Open(Booking.CustomerPhone!);
        }
        catch (Exception)
        {
            // No dialler on this device — nothing useful to say about it mid-sheet.
        }

        await Task.CompletedTask;
    }

    // A double tap on a 56px row is one tap too many when the first one already started the work.
    private async Task GuardAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }

    private static string DescribeBooked(DateTimeOffset createdAt)
    {
        if (createdAt == default)
            return "—";

        var age = DateTimeOffset.UtcNow - createdAt;
        if (age.TotalMinutes < 60) return $"{Math.Max(1, (int)age.TotalMinutes)} min ago";
        if (age.TotalHours < 24) return (int)age.TotalHours == 1 ? "1 hour ago" : $"{(int)age.TotalHours} hours ago";
        var days = (int)age.TotalDays;
        return days == 1 ? "Yesterday" : $"{days} days ago";
    }
}
