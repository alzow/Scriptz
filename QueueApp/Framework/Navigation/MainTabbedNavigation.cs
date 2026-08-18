using QueueApp.Constants;
using QueueApp.Services.Api.Business;

namespace QueueApp.Framework.Navigation;

public static class MainTabbedNavigation
{
    public static async Task<bool> TryGetOwnedBusinessAsync(IBusinessService businessService)
    {
        try
        {
            var businessId = await businessService.GetOwnedBusinessIdAsync();
            return businessId != Guid.Empty;
        }
        catch
        {
            return false; // no owned business, or the lookup failed — either way, no Manage tab
        }
    }

    public static string BuildMainTabbedUri(bool includeManageTab)
    {
        var uri = $"/{NavigationPaths.MainTabbedPage}" +
                  $"?{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.CategoryPickerPage}" +
                  $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.HistoryPage}" +
                  $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.ProfilePage}";

        if (includeManageTab)
            uri += $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{NavigationPaths.OperatorQueuePage}";

        uri += $"&{KnownNavigationParameters.SelectTab}={NavigationPaths.CategoryPickerPage}";

        return uri;
    }
}
