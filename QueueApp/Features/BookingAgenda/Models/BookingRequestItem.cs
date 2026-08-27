using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed class BookingRequestItem : ObservableObject
{
    public required AgendaBookingResponse Booking { get; init; }

    public string CustomerName => Booking.CustomerName;
    public string WhenText { get; init; } = string.Empty;
    public string DetailText { get; init; } = string.Empty;

    public string ConflictText { get; init; } = string.Empty;
    public bool HasConflict => ConflictText.Length > 0;

    public bool IsConfirming { get; set; }
    public bool IsDeclining { get; set; }
    public bool IsBusy => IsConfirming || IsDeclining;
    public bool IsEnabled => !IsBusy;

    public static BookingRequestItem From(
        AgendaBookingResponse booking,
        IReadOnlyList<AvailabilityBlockResponse> blocks,
        IReadOnlyDictionary<Guid, string> operatorNames)
    {
        var details = new List<string>(3);
        if (booking.ServiceName.Length > 0) details.Add(booking.ServiceName);
        if (booking.Operator is not null) details.Add(booking.Operator.DisplayName);
        if (booking.PriceText.Length > 0) details.Add(booking.PriceText);

        return new BookingRequestItem
        {
            Booking = booking,
            WhenText = booking.DayAndRangeDisplay,
            DetailText = string.Join(" · ", details),
            ConflictText = DescribeConflict(booking, blocks, operatorNames),
        };
    }

    public static string DescribeConflict(
        AgendaBookingResponse booking,
        IReadOnlyList<AvailabilityBlockResponse> blocks,
        IReadOnlyDictionary<Guid, string> operatorNames)
    {
        if (booking.OperatorId is null)
            return string.Empty;

        var clash = blocks.FirstOrDefault(b =>
            b.OperatorId == booking.OperatorId &&
            b.StartsAt < booking.EndsAt &&
            b.EndsAt > booking.StartsAt);

        if (clash is null)
            return string.Empty;

        var who = operatorNames.TryGetValue(clash.OperatorId, out var name) ? name : "this resource";
        var reason = string.IsNullOrWhiteSpace(clash.Reason) ? "blocked time" : clash.Reason;

        return $"Falls inside {who}'s {reason}";
    }
}
