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

    public static string BuildMainTabbedUri(bool includeManageTab, string? manageMode = null)
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

        uri += $"&{KnownNavigationParameters.SelectTab}={NavigationPaths.CategoryPickerPage}";

        return uri;
    }
}
