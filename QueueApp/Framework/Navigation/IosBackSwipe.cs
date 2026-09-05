#if IOS
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
#endif

namespace QueueApp.Framework.Navigation;

/// <summary>
/// Turns iOS's own swipe-to-go-back off on every page.
/// </summary>
public static class IosBackSwipe
{
    // The swipe pops the view controller inside UIKit and MAUI follows along, but MPowerKit is
    // never told: the page it still has on its record has gone, and from then on its record and the
    // real stack disagree. Nothing fails at the swipe itself — the crash lands on the next
    // navigation that reads the record, which is the dismissal that returns to the tabs, one screen
    // and several taps later.
    //
    // Turned off rather than reconciled because there is no way to tell the library its stack moved
    // without it. Nothing is lost on screen: the native navigation bar is hidden app-wide and every
    // page draws its own chevron, which goes back the way the library expects.
    public static void Disable()
    {
#if IOS
        PageHandler.Mapper.AppendToMapping(nameof(IosBackSwipe), (_, view) =>
        {
            if (view is not Page page)
                return;

            // Not done here: a page's view controller has no navigation controller to read until it
            // has been pushed onto one, which is Loaded rather than handler creation. Unsubscribed
            // first because the mapper runs again every time a page is rebound to a handler.
            page.Loaded -= OnPageLoaded;
            page.Loaded += OnPageLoaded;
        });
#endif
    }

#if IOS
    private static void OnPageLoaded(object? sender, EventArgs e)
    {
        if (sender is not Page page || page.Handler is not IPlatformViewHandler handler)
            return;

        if (handler.ViewController?.NavigationController is not { } controller)
            return;

        if (controller.InteractivePopGestureRecognizer is { } edgeSwipe)
            edgeSwipe.Enabled = false;

        // iOS 26 added a second recogniser that drives the same pop from a swipe anywhere in the
        // content rather than from the edge. Reading it on an older iOS throws, hence the check.
        if (OperatingSystem.IsIOSVersionAtLeast(26) &&
            controller.InteractiveContentPopGestureRecognizer is { } contentSwipe)
            contentSwipe.Enabled = false;
    }
#endif
}
