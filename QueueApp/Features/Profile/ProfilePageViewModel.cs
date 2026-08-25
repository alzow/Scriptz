using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Profile;

public class ProfilePageViewModel : BaseViewModel
{
    public ProfilePageViewModel(INavigationService navigationService, ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            // TODO: profile info (name, phone once phone-OTP lands), T&Cs link,
            // and the "Become an operator" entry point — registering a business,
            // which per 4d's note will need a re-navigation into MainTabbedPage
            // to pick up the new Manage tab once that flow exists.
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
