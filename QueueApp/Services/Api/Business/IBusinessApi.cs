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
        [AliasAs("select")] string select = "*");

    [Get("/businesses")]
    Task<List<BusinessResponse>> GetBusinessesByCategoryAsync(
        [AliasAs("category")] string categoryEq,
        [AliasAs("suburb")] string suburbEq,
        [AliasAs("select")] string select = "*");

    // Presence heartbeat
    [Patch("/businesses")]
    Task HeartbeatAsync(
        [AliasAs("id")] string idEq,
        [Body] Dictionary<string, object> patch);

    // Browse dashboard list — wait/occupancy aggregate already attached per business.
    [Post("/rpc/nearby_business_summary")]
    Task<List<BrowseBusinessSummaryResponse>> GetBrowseBusinessesAsync([Body] NearbyBusinessSummaryRequest request);
}
