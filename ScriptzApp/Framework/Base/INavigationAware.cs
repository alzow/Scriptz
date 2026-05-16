using MPowerKit.Navigation;

namespace ScriptzApp.Framework.Base;

public interface INavigationAware
{
    void OnNavigatedTo(INavigationParameters parameters);
    void OnNavigatedFrom(INavigationParameters parameters);
}
