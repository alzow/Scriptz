using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Profile.Models;

namespace QueueApp.Services.Stubs;

public class StubProfileService : IProfileService
{
    public Task<string> GetMyDisplayNameAsync(Guid userId)
        => Task.FromResult("Customer");

    public Task<ProfileResponse?> GetMyProfileAsync(Guid userId)
        => Task.FromResult<ProfileResponse?>(new ProfileResponse { Id = userId, DisplayName = "Customer" });
}
