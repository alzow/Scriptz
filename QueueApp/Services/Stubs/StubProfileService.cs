using QueueApp.Services.Api.Profile;

namespace QueueApp.Services.Stubs;

public class StubProfileService : IProfileService
{
    public Task<string> GetMyDisplayNameAsync(Guid userId)
        => Task.FromResult("Customer");
}
