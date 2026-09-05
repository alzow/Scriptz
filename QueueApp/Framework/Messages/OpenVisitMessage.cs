namespace QueueApp.Framework.Messages;

// Sent when a tapped push notification names a queue entry or a booking. It goes to the tabbed
// page for the same reason SelectTabMessage does: VisitPage is pushed modally over the tabs, and
// the tabbed page's own navigation service is the only one that can do that.
public record OpenVisitMessage(Guid RecordId, bool IsBooking);
