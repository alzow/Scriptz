using QueueApp.Framework.Base;

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
        var rows = await ExecuteApiCallAsync(_api.GetProfileByIdAsync($"eq.{userId}"));
        var name = rows.FirstOrDefault()?.DisplayName;
        return string.IsNullOrWhiteSpace(name) ? "Customer" : name;
    }
}
