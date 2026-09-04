using System.Diagnostics;
using Refit;

namespace QueueApp.Framework.Base;

// Wraps Refit calls so every service gets consistent failure logging without repeating try/catch
// boilerplate. Rethrows, so the view model's HandleExceptionAsync still owns the message shown.
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

    // A PostgREST read of one row still comes back as a list, and every service was unwrapping it
    // the same way.
    protected async Task<T?> ExecuteSingleAsync<T>(Task<List<T>> apiCall)
    {
        var rows = await ExecuteApiCallAsync(apiCall);
        return rows.Count > 0 ? rows[0] : default;
    }

    private static void LogFailure(Exception exception)
    {
        var requestUri = exception is ApiException apiEx ? apiEx.RequestMessage?.RequestUri?.ToString() : null;
        Debug.WriteLine($"API call failed: {exception.Message} Request URI: {requestUri}");
    }
}
