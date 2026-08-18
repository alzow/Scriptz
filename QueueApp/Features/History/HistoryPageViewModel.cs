using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Services.Storage;

namespace QueueApp.Features.History;

public class HistoryPageViewModel : BaseViewModel
{
    public HistoryPageViewModel(INavigationService navigationService, ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            // TODO: load the signed-in user's past `visits` once that query exists.
            // Contains your queue histories — past visits, no-shows, completed services.
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
