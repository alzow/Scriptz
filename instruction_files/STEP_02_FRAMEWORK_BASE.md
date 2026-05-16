# STEP 2: Create Framework Base Classes

This step creates the MVVM foundation (BaseViewModel and interfaces).

## Create Directory Structure:

```bash
mkdir -p Framework/Base
mkdir -p Framework/Extensions
```

## Create Framework/Base/INavigationAware.cs

```csharp
using MPowerKit.Navigation;

namespace ScriptzApp.Framework.Base;

public interface INavigationAware
{
    void OnNavigatedTo(NavigationParameters parameters);
    void OnNavigatedFrom(NavigationParameters parameters);
}
```

## Create Framework/Base/IInitializeAware.cs

```csharp
using MPowerKit.Navigation;

namespace ScriptzApp.Framework.Base;

public interface IInitializeAware
{
    void Initialize(NavigationParameters parameters);
}
```

## Create Framework/Base/ILoadedAsyncAware.cs

```csharp
using MPowerKit.Navigation;

namespace ScriptzApp.Framework.Base;

public interface ILoadedAsyncAware
{
    Task OnLoadedAsync(NavigationParameters parameters);
}
```

## Create Framework/Base/IPageLifecycleAware.cs

```csharp
namespace ScriptzApp.Framework.Base;

public interface IPageLifecycleAware
{
    void OnAppearing();
    void OnDisappearing();
    Task OnAppearingAsync();
    Task OnDisappearingAsync();
}
```

## Create Framework/Base/BaseViewModel.cs

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using MPowerKit.Navigation;
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
    public virtual void OnNavigatedTo(NavigationParameters parameters)
    {
    }

    public virtual void OnNavigatedFrom(NavigationParameters parameters)
    {
    }
    #endregion

    #region IInitializeAware
    public virtual void Initialize(NavigationParameters parameters)
    {
    }
    #endregion

    #region ILoadedAsyncAware
    public virtual Task OnLoadedAsync(NavigationParameters parameters)
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
```

**STOP HERE - Confirm all files are created before proceeding to Step 3**
