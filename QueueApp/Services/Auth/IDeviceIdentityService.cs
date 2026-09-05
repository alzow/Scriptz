namespace QueueApp.Services.Auth;

public interface IDeviceIdentityService
{
    Task<string> GetDeviceIdAsync();
}