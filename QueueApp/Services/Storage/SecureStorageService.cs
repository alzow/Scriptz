namespace ScriptzApp.Services.Storage;

public class SecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage Get error: {ex.Message}");
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage Set error: {ex.Message}");
        }
    }

    public Task<bool> RemoveAsync(string key)
    {
        try
        {
            return Task.FromResult(SecureStorage.Default.Remove(key));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage Remove error: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task RemoveAllAsync()
    {
        try
        {
            SecureStorage.Default.RemoveAll();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage RemoveAll error: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
