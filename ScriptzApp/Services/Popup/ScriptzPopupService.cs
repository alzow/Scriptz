using MPowerKit.Popups.Interfaces;

namespace ScriptzApp.Services.Popup;

public class ScriptzPopupService : IScriptzPopupService
{
    private readonly IPopupService _popupService;

    public ScriptzPopupService(IPopupService popupService)
    {
        _popupService = popupService;
    }

    public async Task ShowAlertAsync(string title, string message, string button = "OK")
    {
        await Application.Current!.MainPage!.DisplayAlert(title, message, button);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        return await Application.Current!.MainPage!.DisplayAlert(title, message, accept, cancel);
    }

    public Task ShowLoadingAsync(string message = "Loading...")
    {
        return Task.CompletedTask;
    }

    public Task HideLoadingAsync()
    {
        return Task.CompletedTask;
    }
}
