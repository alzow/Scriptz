using MPowerKit.Popups;

namespace QueueApp.Services.Popup;

public interface IQueuePopupService
{
    Task ShowAlertAsync(string title, string message, string button = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task<string?> ShowPromptAsync(string title, string message, string? initialValue = null,
        string accept = "Save", string cancel = "Cancel", string placeholder = "");
    Task ShowLoadingAsync(string message = "Loading...");
    Task HideLoadingAsync();

    // Bottom sheets. A sheet raises its own result and then closes itself through HideSheetAsync,
    // so callers await the sheet's completion rather than this call.
    Task ShowSheetAsync(PopupPage sheet);
    Task HideSheetAsync(PopupPage sheet);
}
