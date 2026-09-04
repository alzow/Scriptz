using QueueApp.Framework.Base;
using QueueApp.Services.Api;
using QueueApp.Services.Api.Profile.Models;

namespace QueueApp.Services.Api.Profile;

public class ProfileService : BaseService, IProfileService
{
    private const string FallbackDisplayName = "Customer";

    // The signed-in user's own row, which nothing but this service writes. Reading it is on the
    // path of every join, every booking and the profile tab, and it changes only when the customer
    // edits it here — so it is held rather than re-fetched, and dropped on that edit.
    private ProfileResponse? _cached;
    private Guid _cachedUserId;

    private readonly IProfileApi _api;

    public ProfileService(IProfileApi api)
    {
        _api = api;
    }

    public async Task<string> GetMyDisplayNameAsync(Guid userId)
    {
        var profile = await GetMyProfileAsync(userId);

        return string.IsNullOrWhiteSpace(profile?.DisplayName)
            ? FallbackDisplayName
            : profile.DisplayName;
    }

    public async Task<ProfileResponse?> GetMyProfileAsync(Guid userId)
    {
        if (_cached is not null && _cachedUserId == userId)
            return _cached;

        var profile = await ExecuteSingleAsync(_api.GetProfileByIdAsync(PostgrestFilter.Eq(userId)));

        _cached = profile;
        _cachedUserId = userId;

        return profile;
    }

    public async Task UpdateMyProfileAsync(Guid userId, string? displayName, string? phone)
    {
        await ExecuteApiCallAsync(_api.UpdateProfileAsync(PostgrestFilter.Eq(userId), new UpdateProfileRequest
        {
            DisplayName = displayName,
            Phone = phone,
        }));

        InvalidateCache();
    }

    public void InvalidateCache()
    {
        _cached = null;
        _cachedUserId = Guid.Empty;
    }
}
