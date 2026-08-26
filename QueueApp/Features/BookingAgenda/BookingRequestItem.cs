using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BookingAgenda;

// One pending booking, as it reads inside the requests banner. A pending booking is a customer who
// can't plan their day, so the age of the ask is on the row, not buried.
public sealed class BookingRequestItem : ObservableObject
{
    public required AgendaBookingResponse Booking { get; init; }

    public string CustomerName => Booking.CustomerName;
    public string WhenText { get; init; } = "";
    public string DetailText { get; init; } = "";

    // bookings_no_overlap is a gist exclusion on operator_id and time, for pending and confirmed
    // rows only — it has never heard of availability_blocks. A request can legitimately land inside
    // a blocked morning and the database will accept the confirm without a word, so the overlap is
    // checked here instead (spec §4).
    public string ConflictText { get; init; } = "";
    public bool HasConflict => ConflictText.Length > 0;

    public bool IsConfirming { get; set; }
    public bool IsDeclining { get; set; }
    public bool IsBusy => IsConfirming || IsDeclining;

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

    private static string DescribeConflict(
        AgendaBookingResponse booking,
        IReadOnlyList<AvailabilityBlockResponse> blocks,
        IReadOnlyDictionary<Guid, string> operatorNames)
    {
        if (booking.OperatorId is null)
            return "";

        var clash = blocks.FirstOrDefault(b =>
            b.OperatorId == booking.OperatorId &&
            b.StartsAt < booking.EndsAt &&
            b.EndsAt > booking.StartsAt);

        if (clash is null)
            return "";

        var who = operatorNames.TryGetValue(clash.OperatorId, out var name) ? name : "this resource";
        var reason = string.IsNullOrWhiteSpace(clash.Reason) ? "blocked time" : clash.Reason;

        return $"Falls inside {who}'s {reason}";
    }
}
