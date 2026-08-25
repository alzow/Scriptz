namespace QueueApp.Services.Popup;

public interface IQueuePopupService
{
    Task ShowAlertAsync(string title, string message, string button = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task ShowLoadingAsync(string message = "Loading...");
    Task HideLoadingAsync();
}
