using QueueApp.Services.Storage;

namespace QueueApp.Services.Auth;

public class DeviceIdentityService : IDeviceIdentityService
{
    private const string DeviceIdKey = "queue_device_id";

    private readonly ISecureStorageService _secureStorage;

    public DeviceIdentityService(ISecureStorageService secureStorage)
        => _secureStorage = secureStorage;

    public async Task<string> GetDeviceIdAsync()
    {
        var existing = await _secureStorage.GetAsync(DeviceIdKey);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var deviceId = Guid.NewGuid().ToString("N");
        await _secureStorage.SetAsync(DeviceIdKey, deviceId);
        return deviceId;
    }
}