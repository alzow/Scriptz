using CommunityToolkit.Mvvm.Messaging;
using MPowerKit.Navigation;
using QueueApp.Framework.Base;
using QueueApp.Framework.Messages;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Main;

public partial class MainTabbedPageViewModel : BaseViewModel,
    IRecipient<NavigateAwayFromTabsMessage>,
    IRecipient<SelectTabMessage>
{
    private readonly IMessenger _messenger;

    public MainTabbedPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IMessenger messenger)
        : base(navigationService, secureStorageService)
    {
        _messenger = messenger;

        // Registered for the life of the view model rather than between Appearing and Disappearing:
        // pushing a page modally raises Disappearing on the page it covers, so a registration tied
        // to that would be gone for exactly the stretch the modal is up and needs to talk back.
        // WeakReferenceMessenger holds recipients weakly, so this goes when the page does.
        _messenger.Register<NavigateAwayFromTabsMessage>(this);
        _messenger.Register<SelectTabMessage>(this);
    }

    // Modal, not absolute. The page gets the whole window either way, but a modal leaves this
    // tabbed page and all four of its tabs standing underneath, so coming back is a pop rather than
    // rebuilding every tab and reloading every feed from scratch.
    public void Receive(NavigateAwayFromTabsMessage message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await NavigationService.NavigateAsync(
                    message.NavigationPath, message.Parameters, modal: true, animated: false);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
            }
        });
    }

    // Selecting a tab is only ever the tabbed page's own navigation service's job, so a page on its
    // way out of the modal asks for it from here. It works while the modal is still up, which is
    // what lets the tab change happen behind the dismissal rather than visibly after it.
    public void Receive(SelectTabMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // SelectTab reports failure rather than no-oping when the tab asked for is already
                // the current one, which is the common case for a page that came out of that tab.
                NavigationService.SelectTab(message.TabName, null);
            }
            catch (Exception ex)
            {
                _ = HandleExceptionAsync(ex);
            }
        });
    }
}
