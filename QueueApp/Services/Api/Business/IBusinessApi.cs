using Refit;
using QueueApp.Services.Api.Business.Models;

namespace QueueApp.Services.Api.Business;

public interface IBusinessApi
{
    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    [Get("/businesses")]
    Task<List<BusinessIdResponse>> GetOwnedBusinessesAsync(
        [AliasAs("owner_id")] string ownerIdEq,
        [AliasAs("select")] string select = "id");

    [Get("/businesses")]
    Task<List<BusinessResponse>> GetBusinessesAsync(
        [AliasAs("id")] string idEq,
        [AliasAs("select")] string select = "id,name");

    // Presence heartbeat
    [Patch("/businesses")]
    Task HeartbeatAsync(
        [AliasAs("id")] string idEq,
        [Body] Dictionary<string, object> patch);
}
