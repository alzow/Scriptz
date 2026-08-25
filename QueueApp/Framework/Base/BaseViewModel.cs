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

    // A Postgres RAISE EXCEPTION surfaces through PostgREST as a JSON body like
    // {"message": "...", ...} — Refit's ApiException.Message is just generic HTTP status text,
    // so a VM that wants to show the real reason (e.g. "all resources are currently busy") to
    // the user needs this instead. Falls back to the exception's own message when there's
    // nothing to parse (not an API error, or the body isn't the shape above).
    protected static string GetFriendlyErrorMessage(Exception exception)
    {
        if (exception is Refit.ApiException { Content: { Length: > 0 } content })
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("message", out var messageEl) &&
                    messageEl.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var message = messageEl.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Not JSON — fall through to the exception's own message.
            }
        }

        return exception.Message;
    }
}
