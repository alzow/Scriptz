namespace QueueApp.Services.Storage;

public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<bool> RemoveAsync(string key);
    Task RemoveAllAsync();
}
