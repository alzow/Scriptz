using MPowerKit.Navigation;

namespace ScriptzApp.Framework.Base;

public interface IInitializeAware
{
    void Initialize(INavigationParameters parameters);
}
