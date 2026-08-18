using CommunityToolkit.Mvvm.Messaging;
using MPowerKit.Navigation;
using QueueApp.Framework.Base;
using QueueApp.Framework.Messages;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Main;

public partial class MainTabbedPageViewModel : BaseViewModel, IRecipient<NavigateAwayFromTabsMessage>
{
    private readonly IMessenger _messenger;

    public MainTabbedPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IMessenger messenger)
        : base(navigationService, secureStorageService)
    {
        _messenger = messenger;
        _messenger.Register<NavigateAwayFromTabsMessage>(this);
    }

    public void Receive(NavigateAwayFromTabsMessage message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await NavigationService.NavigateAsync(message.NavigationPath, message.Parameters, animated: message.IsAnimated);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
            }
        });
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        _messenger.Unregister<NavigateAwayFromTabsMessage>(this);
    }
}
