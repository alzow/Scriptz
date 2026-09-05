using System.Text.Json;
using QueueApp.Constants;

namespace QueueApp.Services.Notifications;

public sealed record PushNotificationRoute(string Action, Guid RecordId)
{
    public bool IsManageTab =>
        Action is PushNotificationActions.OperatorQueue or PushNotificationActions.OperatorBookings;

    public bool IsVisit =>
        Action is PushNotificationActions.QueueStatus or PushNotificationActions.BookingDetail;

    public bool IsBooking => Action == PushNotificationActions.BookingDetail;

    // The action already says which board the shop was told about, so the tab is picked from it
    // rather than from another read of the owned business — a shop only gets bookings notices in
    // booking mode and queue notices in queue mode.
    public string ManageTabName => Action == PushNotificationActions.OperatorBookings
        ? NavigationPaths.BookingAgendaPage
        : NavigationPaths.OperatorQueuePage;

    public static PushNotificationRoute? From(IDictionary<string, string>? data)
    {
        if (data is null
            || !data.TryGetValue(PushNotificationKeys.Action, out var action)
            || string.IsNullOrWhiteSpace(action))
            return null;

        return action switch
        {
            PushNotificationActions.OperatorQueue or PushNotificationActions.OperatorBookings =>
                new PushNotificationRoute(action, Guid.Empty),

            PushNotificationActions.QueueStatus =>
                VisitRoute(action, ReadId(data, PushNotificationKeys.EntryId)),

            PushNotificationActions.BookingDetail =>
                VisitRoute(action, ReadId(data, PushNotificationKeys.BookingId)),

            _ => null,
        };
    }

    private static PushNotificationRoute? VisitRoute(string action, Guid recordId) =>
        recordId == Guid.Empty ? null : new PushNotificationRoute(action, recordId);

    private static Guid ReadId(IDictionary<string, string> data, string key)
    {
        if (data.TryGetValue(key, out var flat) && Guid.TryParse(flat, out var flatId))
            return flatId;

        if (!data.TryGetValue(PushNotificationKeys.ActionParams, out var json) || string.IsNullOrWhiteSpace(json))
            return Guid.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);

            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(key, out var element)
                && element.ValueKind == JsonValueKind.String
                && Guid.TryParse(element.GetString(), out var id)
                    ? id
                    : Guid.Empty;
        }
        catch (JsonException)
        {
            return Guid.Empty;
        }
    }
}
