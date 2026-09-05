using MPowerKit.Popups;
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

    // Returns null when dismissed, which is distinct from "" — clearing a note is a real edit.
    public async Task<string?> ShowPromptAsync(string title, string message, string? initialValue = null,
        string accept = "Save", string cancel = "Cancel", string placeholder = "")
    {
        return await Application.Current!.MainPage!.DisplayPromptAsync(
            title, message, accept, cancel, placeholder, initialValue: initialValue ?? string.Empty);
    }

    // DisplayActionSheet answers with the cancel text itself when dismissed on some platforms and
    // with null on others; both mean "no choice", so both come back as null.
    public async Task<string?> ShowActionSheetAsync(string title, string cancel, params string[] options)
    {
        var chosen = await Application.Current!.MainPage!.DisplayActionSheet(title, cancel, null, options);
        return string.IsNullOrEmpty(chosen) || chosen == cancel ? null : chosen;
    }

    public Task ShowLoadingAsync(string message = "Loading...")
    {
        return Task.CompletedTask;
    }

    public Task HideLoadingAsync()
    {
        return Task.CompletedTask;
    }

    public Task ShowSheetAsync(PopupPage sheet) => _popupService.ShowPopupAsync(sheet).AsTask();

    // Tolerates a sheet that is already gone (dismissed by a background tap, then closed again by
    // its own handler) rather than throwing out of a UI gesture.
    public Task HideSheetAsync(PopupPage sheet)
    {
        if (sheet.IsClosing || !_popupService.PopupStack.Contains(sheet))
            return Task.CompletedTask;

        return _popupService.HidePopupAsync(sheet).AsTask();
    }
}
