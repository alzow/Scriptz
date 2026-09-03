namespace QueueApp.Services.Api.Booking.Models;

// The `booking_status` enum's actual labels were never captured by the schema-verification query
// (SUPABASE-SCHEMA-VERIFIED.md §1g lists it as "confirmed enum, labels not pulled"), so these are
// the labels the app has always sent and read, plus the two the agenda needs and the enum may not
// have yet. See Documentation/STEP-18-BOOKING-AGENDA-SUPABASE.md — until that migration is applied,
// InProgress and NoShow simply never come back from the server and the UI degrades to "not started"
// rather than showing something untrue.
public static class BookingStatuses
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string NoShow = "no_show";

    // TODO: stub pending Documentation/awaiting-collection-backend-requirements.md — swap this
    // constant for the real enum label once that spec lands.
    public const string AwaitingCollection = "awaiting_collection";

    // What the day's revenue figure counts. A cancelled or no-show booking earned nothing, so
    // including it would make the number worse than useless (spec §3).
    public static bool CountsTowardsRevenue(string status) =>
        status is not (Cancelled or NoShow);

    // A slot that is still, or was, genuinely occupied — used for "Booked" counts and for working
    // out which bookings a new availability block would collide with.
    public static bool OccupiesTheDiary(string status) =>
        status is not (Cancelled or NoShow);
}
