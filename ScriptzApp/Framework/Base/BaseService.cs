using System.Diagnostics;
using Refit;

namespace ScriptzApp.Framework.Base;

// Wraps Refit calls so every service gets consistent failure logging without repeating
// try/catch boilerplate. Rethrows so BaseViewModel.ExecuteAsync/HandleExceptionAsync can
// still surface the error to the user.
public abstract class BaseService
{
    protected async Task<T> ExecuteApiCallAsync<T>(Task<T> apiCall)
    {
        try
        {
            return await apiCall;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or IOException)
        {
            LogFailure(ex);
            throw;
        }
    }

    protected async Task ExecuteApiCallAsync(Task apiCall)
    {
        try
        {
            await apiCall;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or IOException)
        {
            LogFailure(ex);
            throw;
        }
    }

    private static void LogFailure(Exception exception)
    {
        var requestUri = exception is ApiException apiEx ? apiEx.RequestMessage?.RequestUri?.ToString() : null;
        Debug.WriteLine($"API call failed: {exception.Message} Request URI: {requestUri}");
    }
}
