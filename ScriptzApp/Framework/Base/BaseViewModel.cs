using CommunityToolkit.Mvvm.ComponentModel;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using ScriptzApp.Services.Storage;

namespace ScriptzApp.Framework.Base;

public abstract class BaseViewModel : ObservableObject,
    INavigationAware,
    IInitializeAware,
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

    #region ILoadedAsyncAware
    public virtual Task OnLoadedAsync(INavigationParameters parameters)
    {
        return Task.CompletedTask;
    }
    #endregion

    #region IPageLifecycleAware
    public virtual void OnAppearing()
    {
    }

    public virtual void OnDisappearing()
    {
    }

    public virtual Task OnAppearingAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnDisappearingAsync()
    {
        return Task.CompletedTask;
    }
    #endregion

    protected async Task ExecuteAsync(Func<Task> operation, string? loadingMessage = null)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await operation();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected virtual Task HandleExceptionAsync(Exception exception)
    {
        System.Diagnostics.Debug.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}
