using QueueApp.Services.Api.Auth.Models;
using Refit;

namespace QueueApp.Services.Api.Auth;

public interface IDeviceTokenApi
{
    [Post("/rpc/upsert_device_token")]
    Task UpsertAsync([Body] UpsertDeviceTokenRequest request);

    [Post("/rpc/remove_device_token")]
    Task RemoveAsync([Body] RemoveDeviceTokenRequest request);
}