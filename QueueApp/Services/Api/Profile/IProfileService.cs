using QueueApp.Services.Api.Profile.Models;

namespace QueueApp.Services.Api.Profile;

public interface IProfileService
{
    Task<string> GetMyDisplayNameAsync(Guid userId);

    Task<ProfileResponse?> GetMyProfileAsync(Guid userId);

    Task UpdateMyProfileAsync(Guid userId, string? displayName, string? phone);

    // Drops the held profile so the next read goes to the server — for a sign-out, where the next
    // signed-in user must not be handed the last one's row.
    void InvalidateCache();
}
