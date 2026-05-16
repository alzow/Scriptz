using MPowerKit.Navigation;

namespace ScriptzApp.Framework.Base;

public interface ILoadedAsyncAware
{
    Task OnLoadedAsync(INavigationParameters parameters);
}
