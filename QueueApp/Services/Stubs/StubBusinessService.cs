using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real BusinessService in DEBUG builds.
public class StubBusinessService : IBusinessService
{
    private readonly Guid _defaultBusinessId = new("0637f5ef-c7fa-46dc-b4e5-b814f2d7d3bf");

    public Task<Guid> GetOwnedBusinessIdAsync() => Task.FromResult(_defaultBusinessId);

    public Task<BusinessResponse?> GetBusinessAsync(Guid businessId)
        => Task.FromResult<BusinessResponse?>(new BusinessResponse { Id = businessId, Name = "My Test Barber" });

    public Task HeartbeatAsync(Guid businessId) => Task.CompletedTask;
}
