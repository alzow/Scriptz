using CommunityToolkit.Mvvm.ComponentModel;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Services.Storage;

namespace QueueApp.Framework.Base;

public abstract class BaseViewModel : ObservableObject,
    INavigationAware,
    IInitializeAware,
    IInitializeAsyncAware,
    ILoadedAsyncAware,
    IPageLifecycleAware
{
    protected INavigationService NavigationService { get; }
    protected ISecureStorageService SecureStorageService { get; }

    public bool IsBusy { get; set; }
    public string Title { get; set; } = string.Empty;

    protected BaseViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService)
    {
        NavigationService = navigationService;
        SecureStorageService = secureStorageService;
    }

    #region INavigationAware
    public virtual void OnNavigatedTo(INavigationParameters parameters)
    {
    }

    public virtual void OnNavigatedFrom(INavigationParameters parameters)
    {
    }
    #endregion

    #region IInitializeAware
    public virtual void Initialize(INavigationParameters parameters)
    {
    }
    #endregion

    #region IInitializeAsyncAware
    public virtual Task InitializeAsync(INavigationParameters parameters)
    {
        return Task.CompletedTask;
    }
    #endregion

    #region ILoadedAsyncAware
    public virtual Task OnLoadedAsync(INavigationParameters parameters)
    {
        return Task.CompletedTask;
    }
    #endregion

    #region IPageLifecycleAware
    public void OnAppearing()
    {
        _ = SafeFireAndForgetAsync(OnAppearingAsync);
    }

    public void OnDisappearing()
    {
        _ = SafeFireAndForgetAsync(OnDisappearingAsync);
    }

    // MPowerKit's PageLifecycleAwareBehavior calls OnAppearing/OnDisappearing synchronously off the
    // native Page.Appearing/Disappearing events, so async work here can never be awaited by the caller.
    // These give derived VMs an awaitable-looking hook with exceptions routed through HandleExceptionAsync
    // instead of each VM hand-rolling its own fire-and-forget.
    public virtual Task OnAppearingAsync() => Task.CompletedTask;
    public virtual Task OnDisappearingAsync() => Task.CompletedTask;

    private async Task SafeFireAndForgetAsync(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    #endregion

    protected virtual Task HandleExceptionAsync(Exception exception)
    {
        System.Diagnostics.Debug.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}
