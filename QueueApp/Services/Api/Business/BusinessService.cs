using QueueApp.Framework.Base;
using QueueApp.Services.Api;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Api.Business;

public class BusinessService : BaseService, IBusinessService
{
    private const string NoAuthenticatedUserError =
        "No authenticated user is available for queue ownership lookup.";
    private const string NoOwnedBusinessError = "No business is associated with the current user.";

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
            throw new InvalidOperationException(NoAuthenticatedUserError);

        var business = await ExecuteSingleAsync(_api.GetOwnedBusinessesAsync(PostgrestFilter.Eq(userId), "id"))
            ?? throw new InvalidOperationException(NoOwnedBusinessError);

        return business.Id;
    }

    public Task<BusinessResponse?> GetBusinessAsync(Guid businessId) =>
        ExecuteSingleAsync(_api.GetBusinessesAsync(PostgrestFilter.Eq(businessId)));

    public Task<List<BusinessResponse>> GetBusinessesAsync(string category, string suburb = "Lenasia") =>
        ExecuteApiCallAsync(_api.GetBusinessesByCategoryAsync(PostgrestFilter.Eq(category), PostgrestFilter.Eq(suburb)));

    public Task<List<BrowseBusinessSummaryResponse>> GetBrowseBusinessesAsync(
        string? category, string suburb = "Lenasia", double? customerLatitude = null, double? customerLongitude = null) =>
        ExecuteApiCallAsync(_api.GetBrowseBusinessesAsync(new NearbyBusinessSummaryRequest
        {
            Category = category,
            Suburb = suburb,
            CustomerLatitude = customerLatitude,
            CustomerLongitude = customerLongitude,
        }));

    public Task HeartbeatAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.HeartbeatAsync(PostgrestFilter.Eq(businessId),
            new Dictionary<string, object> { ["last_seen_at"] = DateTime.UtcNow }));

    public Task UpdateLocationAsync(Guid businessId, double latitude, double longitude) =>
        ExecuteApiCallAsync(_api.UpdateLocationAsync(PostgrestFilter.Eq(businessId),
            new Dictionary<string, object> { ["latitude"] = latitude, ["longitude"] = longitude }));
}
