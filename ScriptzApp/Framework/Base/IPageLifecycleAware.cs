namespace ScriptzApp.Framework.Base;

public interface IPageLifecycleAware
{
    void OnAppearing();
    void OnDisappearing();
    Task OnAppearingAsync();
    Task OnDisappearingAsync();
}
