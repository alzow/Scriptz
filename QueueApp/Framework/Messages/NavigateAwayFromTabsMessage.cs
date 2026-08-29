using MPowerKit.Navigation;

namespace QueueApp.Framework.Messages;

// Sent by a tab that wants a page over the whole window rather than inside its own stack. The
// tabbed page pushes it modally, so the tabs stay alive underneath and the way back is a pop.
//
// NavigationPath must be relative (e.g. "NavigationPage/BusinessDetailPage"): an absolute path
// replaces the window's root, which is exactly what this used to do and what left nothing to come
// back to.
public record NavigateAwayFromTabsMessage(
    string NavigationPath,
    INavigationParameters? Parameters = null,
    bool IsAnimated = false);

// Sent by a page on its way out of the modal when the tabs underneath should land on a specific
// tab. Only the tabbed page's own navigation service can select a tab, so it goes through the
// messenger like the outbound trip does.
public record SelectTabMessage(string TabName);
