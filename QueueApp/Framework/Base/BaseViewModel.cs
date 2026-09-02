using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Loading;
using QueueApp.Services.Storage;

namespace QueueApp.Framework.Base;

public abstract partial class BaseViewModel : ObservableObject,
    INavigationAware,
    IInitializeAware,
    IInitializeAsyncAware,
    ILoadedAsyncAware,
    IPageLifecycleAware
{
    public const string DefaultLoadFailureText = "That didn't load. Check your connection and try again.";
    public const string TimedOutLoadFailureText = "This is taking longer than it should. Try again.";

    // Three states, not one. A skeleton belongs to the first of them and to nothing else:
    // IsFirstLoading is a cold open with no content behind it, IsRefreshing is a pull on content
    // that is already there, and IsBusy is a commit where the layout already exists and only a
    // button changes.
    //
    // The first and the failure flag are hand-written rather than Fody-woven so that both can run
    // OnLoadStateChanged. A derived VM's ShowList or IsEmpty almost always depends on them, and
    // Fody cannot see a dependency that crosses into a base class.
    public bool IsFirstLoading
    {
        get => _isFirstLoading;
        private set
        {
            if (SetProperty(ref _isFirstLoading, value))
                OnLoadStateChanged();
        }
    }

    public bool HasLoadFailed
    {
        get => _hasLoadFailed;
        private set
        {
            if (SetProperty(ref _hasLoadFailed, value))
                OnLoadStateChanged();
        }
    }

    public bool IsRefreshing { get; set; }
    public bool IsBusy { get; set; }

    public string LoadFailureText { get; private set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    private bool _isFirstLoading;
    private bool _hasLoadFailed;

    protected INavigationService NavigationService { get; }
    protected ISecureStorageService SecureStorageService { get; }

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

    // The three timing rules in one place, so no screen can implement two of them and forget the
    // third. Wait before showing, because most cached loads land inside the delay and never show a
    // skeleton at all; commit for a minimum once shown, so it cannot appear and vanish in the same
    // breath; and give up, because a skeleton that never resolves is the worst of both worlds.
    //
    // The timeout is a race rather than a cancellation token: nothing in Services/Api takes one, so
    // a token would be a promise this cannot keep. The abandoned work still completes and is still
    // logged, it just no longer owns the screen.
    public async Task RunFirstLoadAsync(Func<Task> work)
    {
        HasLoadFailed = false;
        LoadFailureText = string.Empty;

        var shownAt = default(DateTime?);
        var loadTask = work();

        try
        {
            var delayed = await Task.WhenAny(loadTask, Task.Delay(LoadingTiming.ShowDelay));
            if (delayed != loadTask)
            {
                IsFirstLoading = true;
                shownAt = DateTime.UtcNow;
            }

            var finished = await Task.WhenAny(loadTask, Task.Delay(LoadingTiming.Timeout));
            if (finished != loadTask)
            {
                Fail(TimedOutLoadFailureText);
                return;
            }

            await loadTask;
        }
        catch (Exception ex)
        {
            Fail(GetFriendlyErrorMessage(ex));
            await HandleExceptionAsync(ex);
        }
        finally
        {
            if (shownAt is not null)
            {
                var remaining = LoadingTiming.MinimumVisible - (DateTime.UtcNow - shownAt.Value);
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining);
            }

            IsFirstLoading = false;
        }
    }

    // Raised whenever the skeleton appears, disappears or becomes an error. Screens override it to
    // re-raise whatever their list slot is bound to.
    public virtual void OnLoadStateChanged()
    {
    }

    // What a retry re-runs. Screens that can fail a first load override this; the ones that cannot
    // have nothing to retry and inherit the no-op.
    public virtual Task ReloadAsync() => Task.CompletedTask;

    [RelayCommand]
    public async Task RetryLoadAsync()
    {
        try
        {
            HasLoadFailed = false;
            LoadFailureText = string.Empty;
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void Fail(string message)
    {
        HasLoadFailed = true;
        LoadFailureText = string.IsNullOrWhiteSpace(message) ? DefaultLoadFailureText : message;
    }

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
