using QueueApp.Framework.Base;
using QueueApp.Services.Api.Profile.Models;

namespace QueueApp.Services.Api.Profile;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class ProfileService : BaseService, IProfileService
{
    private readonly IProfileApi _api;

    public ProfileService(IProfileApi api)
    {
        _api = api;
    }

    public async Task<string> GetMyDisplayNameAsync(Guid userId)
    {
        var profile = await GetMyProfileAsync(userId);
        var name = profile?.DisplayName;
        return string.IsNullOrWhiteSpace(name) ? "Customer" : name;
    }

    public async Task<ProfileResponse?> GetMyProfileAsync(Guid userId)
    {
        var rows = await ExecuteApiCallAsync(_api.GetProfileByIdAsync($"eq.{userId}"));
        return rows.FirstOrDefault();
    }
}
