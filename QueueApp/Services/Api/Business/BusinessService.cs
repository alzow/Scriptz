using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Api.Business;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class BusinessService : BaseService, IBusinessService
{
    private readonly IBusinessApi _api;
    private readonly IAuthService _authService;

    public BusinessService(IBusinessApi api, IAuthService authService)
    {
        _api = api;
        _authService = authService;
    }

    public async Task<Guid> GetOwnedBusinessIdAsync()
    {
        var userId = await _authService.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("No authenticated user is available for queue ownership lookup.");

        var businesses = await ExecuteApiCallAsync(_api.GetOwnedBusinessesAsync($"eq.{userId}", "id"));
        var business = businesses.FirstOrDefault();
        if (business is null)
            throw new InvalidOperationException("No business is associated with the current user.");

        return business.Id;
    }

    public async Task<BusinessResponse?> GetBusinessAsync(Guid businessId)
    {
        var businesses = await ExecuteApiCallAsync(_api.GetBusinessesAsync($"eq.{businessId}"));
        return businesses.FirstOrDefault();
    }

    public Task HeartbeatAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.HeartbeatAsync($"eq.{businessId}",
            new Dictionary<string, object> { ["last_seen_at"] = DateTime.UtcNow }));
}
