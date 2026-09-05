using CommunityToolkit.Mvvm.Messaging;
using QueueApp.Constants;
using QueueApp.Framework.Messages;
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

        string? managePage = null;
        if (includeManageTab)
        {
            managePage = manageMode == "booking"
                ? NavigationPaths.BookingAgendaPage
                : NavigationPaths.OperatorQueuePage;
            uri += $"&{KnownNavigationParameters.CreateTab}=TabNavigationPage|{managePage}";
        }

        // A Manage tab that was never created can't be selected — fall back to Browse rather than
        // building a uri that selects nothing. With no tab requested, an owner lands on Manage
        // rather than the customer's Browse tab.
        var tab = selectTab == NavigationPaths.BookingAgendaPage || selectTab == NavigationPaths.OperatorQueuePage
            ? (includeManageTab ? selectTab : NavigationPaths.CategoryPickerPage)
            : selectTab ?? managePage ?? NavigationPaths.CategoryPickerPage;

        uri += $"&{KnownNavigationParameters.SelectTab}={tab}";

        return uri;
    }

    // True while a page sits over the tabs rather than in place of them. Read inside the gate, so
    // it cannot be answered against a modal stack another transition is halfway through changing.
    private static bool TabsAreStillBehindUs =>
        Application.Current?.Windows.FirstOrDefault()?.Navigation.ModalStack.Count > 0;

    // The way home from any page a tab opened over the tabs. Those pages are pushed modally, so the
    // tabbed page is still standing behind them and dismissing the modal is the whole journey: no
    // owned-business lookup, no tabs to build, no feed to reload. The rebuild is kept only as the
    // fallback for a window that has no modal on it — a page that got here some other way, or one
    // left over after the shell was replaced.
    public static async Task ReturnToTabsAsync(
        INavigationService navigationService,
        IBusinessService businessService,
        IMessenger? messenger = null,
        string? selectTab = null)
    {
        await NavigationGate.RunAsync(async () =>
        {
            if (TabsAreStillBehindUs)
            {
                // Sent before the dismissal, not after: the tabbed page can switch tabs while it is
                // still covered, so the tab change happens behind the modal instead of flashing once
                // it has gone.
                if (selectTab is not null && messenger is not null)
                    messenger.Send(new SelectTabMessage(selectTab));

                try
                {
                    await navigationService.GoBackAsync(modal: true, animated: false);
                    return;
                }
                catch (Exception exception)
                {
                    // A dismissal that fails strands the user on a page whose back has just stopped
                    // working, which is worse than the cost of the rebuild below — so it falls
                    // through to it rather than passing the failure up. Logged because dismissal is
                    // meant to be the cheap path, and one that throws means the library's record of
                    // the stack and MAUI's have disagreed somewhere upstream.
                    System.Diagnostics.Debug.WriteLine(
                        $"[Navigation] modal dismissal failed, rebuilding the tabs: {exception.Message}");
                }
            }

            var (ownsBusiness, mode) = await TryGetOwnedBusinessAsync(businessService);
            await navigationService.NavigateAsync(BuildMainTabbedUri(ownsBusiness, mode, selectTab));
        });
    }
}
