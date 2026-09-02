namespace QueueApp.Constants;

public static class NavigationKeys
{
    public const string BusinessId      = "businessId";
    public const string OperatorId      = "operatorId";
    public const string OperatorName    = "operatorName";
    public const string ServiceId       = "serviceId";
    public const string Category        = "category";
    public const string DayOfWeek       = "dayOfWeek";
    public const string OpenedFromTabs  = "openedFromTabs";

    // VisitPage loads from an id rather than a handed-over model: the History row that opened it
    // may be stale by the time it is tapped.
    public const string EntryId         = "entryId";
    public const string BookingId       = "bookingId";

    // Set only by the join/booking flow, which lands on the same page the History row opens.
    public const string JustJoined      = "justJoined";

    // Set when the shop itself is driving the booking flow from the agenda rather than a customer
    // booking for themselves. Changes who the booking is for, not how the slots are worked out.
    public const string IsOperatorFlow  = "isOperatorFlow";
    public const string PreferredDate   = "preferredDate";
    public const string PreferredStart  = "preferredStart";

    // Set by an entry point that is not a cold first run — a deep link or a notification open.
    // The splash sends a signed-out customer to sign-in rather than the welcome carousel when it
    // is present, because someone who tapped a link to a specific shop did not ask for the pitch.
    // TODO: set this from the deep-link/notification entry point once one exists.
    public const string BypassWelcome   = "bypassWelcome";

    // A BusinessSnapshot handed from the business landing to the flow it opens, so the flow can skip
    // re-fetching what the page behind it already has. Optional: absent when the flow is opened from
    // anywhere else, and the flow fetches for itself then.
    public const string BusinessSnapshot = "businessSnapshot";
}
