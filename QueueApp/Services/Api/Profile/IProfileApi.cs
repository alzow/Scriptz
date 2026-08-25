using Refit;
using QueueApp.Services.Api.Profile.Models;

namespace QueueApp.Services.Api.Profile;

public interface IProfileApi
{
    // Reads (PostgREST filter syntax, e.g. "eq.<guid>")
    [Get("/profiles?select=id,display_name,phone")]
    Task<List<ProfileResponse>> GetProfileByIdAsync([AliasAs("id")] string idEq);
}
