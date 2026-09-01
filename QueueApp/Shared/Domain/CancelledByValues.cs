namespace QueueApp.Shared.Domain;

// Neither queue_entries nor bookings has a cancelled_by column, so both write these values into
// their details jsonb instead. A cancellation the customer made themselves must never be shown to
// them as the shop letting them down.
public static class CancelledByValues
{
    public const string Customer = "customer";
    public const string Business = "business";
}
