using QueueApp.Constants;
using QueueApp.Services.Api.Business;

namespace QueueApp.Framework.Navigation;

public static class MainTabbedNavigation
{
    public static async Task<(bool ownsBusiness, string? mode)> TryGetOwnedBusinessAsync(IBusinessService businessService)
    {
        try
        {
            var businessId = await businessService.GetOwnedBusinessIdAsync();
            if (businessId == Guid.Empty)
                return (false, null);

            var business = await businessService.GetBusinessAsync(businessId);
            return (true, business?.Mode);
        }
        catch
        {
            return (false, null); // no owned business, or the lookup failed — either way, no Manage tab
        }
    }

    // selectTab lands the user on a specific tab instead of Browse — an operator coming back from
    // the booking flow wants the agenda they left, not the customer's home screen.
    public static string BuildMainTabbedUri(bool includeManageTab, string? manageMode = null, string? selectTab = null)
    {
        var uri = $"/{NavigationPaths.MainTabbedPage}" +
                  $"?{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.CategoryPickerPage}" +
                  $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.HistoryPage}" +
                  $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.ProfilePage}";

        if (includeManageTab)
        {
            var managePage = manageMode == "booking"
                ? NavigationPaths.BookingAgendaPage
                : NavigationPaths.OperatorQueuePage;
            uri += $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{managePage}";
        }

        // A Manage tab that was never created can't be selected — fall back to Browse rather than
        // building a uri that selects nothing.
        var tab = selectTab is null || (selectTab == NavigationPaths.BookingAgendaPage
                                        || selectTab == NavigationPaths.OperatorQueuePage) && !includeManageTab
            ? NavigationPaths.CategoryPickerPage
            : selectTab;

        uri += $"&{KnownNavigationParameters.SelectTab}={tab}";

        return uri;
    }
}
