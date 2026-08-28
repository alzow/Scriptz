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

    // Set when the shop itself is driving the booking flow from the agenda rather than a customer
    // booking for themselves. Changes who the booking is for, not how the slots are worked out.
    public const string IsOperatorFlow  = "isOperatorFlow";
    public const string PreferredDate   = "preferredDate";
    public const string PreferredStart  = "preferredStart";
}
