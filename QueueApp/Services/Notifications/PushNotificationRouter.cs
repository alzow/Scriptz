using CommunityToolkit.Mvvm.Messaging;
using QueueApp.Framework.Messages;
using QueueApp.Framework.Navigation;

namespace QueueApp.Services.Notifications;

// A tap on a push notification can land at any point in the app's life, including before there is
// anything to navigate: a cold start delivers the tap while the splash is still deciding where to
// go, and the absolute navigation that builds the tabs would wipe out anything routed ahead of it.
// So a tap that arrives before the tabs are up is held and replayed once they are, and everything
// after that is routed straight away.
public class PushNotificationRouter : IPushNotificationRouter
{
    private PushNotificationRoute? _pendingRoute;
    private bool _tabsAreReady;

    private readonly IMessenger _messenger;

    public PushNotificationRouter(IMessenger messenger)
    {
        _messenger = messenger;
    }

    public bool HasPendingTap => _pendingRoute is not null;

    public void OnNotificationTapped(IDictionary<string, string>? data)
    {
        try
        {
            var route = PushNotificationRoute.From(data);
            if (route is null)
                return;

            if (_tabsAreReady)
                Route(route);
            else
                _pendingRoute = route;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"[Push] could not read the tapped notification: {exception}");
        }
    }

    public void NotifyTabsReady()
    {
        try
        {
            _tabsAreReady = true;

            var route = Interlocked.Exchange(ref _pendingRoute, null);
            if (route is not null)
                Route(route);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"[Push] could not route the notification that opened the app: {exception}");
        }
    }

    // Both destinations go through the messenger rather than a navigation service of this router's
    // own: only the tabbed page's own navigation service can select one of its tabs or push over
    // them, and nothing else in the app holds it. The tap arrives off whatever thread the platform
    // raised it on, so the hop to the main thread happens here rather than in each recipient.
    private void Route(PushNotificationRoute route)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Whatever the app was showing is not what the notification is about, and a visit
                // pushed over a visit would bury the one the customer tapped.
                await MainTabbedNavigation.DismissAnythingOverTheTabsAsync();

                if (route.IsManageTab)
                {
                    _messenger.Send(new SelectTabMessage(route.ManageTabName));
                    return;
                }

                if (route.IsVisit)
                    _messenger.Send(new OpenVisitMessage(route.RecordId, route.IsBooking));
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"[Push] could not route {route.Action}: {exception}");
            }
        });
    }
}
