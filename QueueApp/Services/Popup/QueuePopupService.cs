using MPowerKit.Popups.Interfaces;

namespace QueueApp.Services.Popup;

public class QueuePopupService : IQueuePopupService
{
    private readonly IPopupService _popupService;

    public QueuePopupService(IPopupService popupService)
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
