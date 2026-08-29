using QueueApp.Services.Api.Profile.Models;

namespace QueueApp.Services.Api.Profile;

public interface IProfileService
{
    Task<string> GetMyDisplayNameAsync(Guid userId);

    Task<ProfileResponse?> GetMyProfileAsync(Guid userId);
}
