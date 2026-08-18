using MPowerKit.Navigation;

namespace QueueApp.Framework.Messages;

public record NavigateAwayFromTabsMessage(
    string NavigationPath,
    INavigationParameters? Parameters = null,
    bool IsAnimated = false);
