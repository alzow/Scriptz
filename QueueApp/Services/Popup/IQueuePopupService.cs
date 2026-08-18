namespace ScriptzApp.Services.Popup;

public interface IScriptzPopupService
{
    Task ShowAlertAsync(string title, string message, string button = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task ShowLoadingAsync(string message = "Loading...");
    Task HideLoadingAsync();
}
