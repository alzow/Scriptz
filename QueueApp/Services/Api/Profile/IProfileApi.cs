using Refit;
using QueueApp.Services.Api.Profile.Models;

namespace QueueApp.Services.Api.Profile;

public interface IProfileApi
{
    [Get("/profiles?select=id,display_name,phone")]
    Task<List<ProfileResponse>> GetProfileByIdAsync([AliasAs("id")] string idEq);

    // Permitted by the "profiles self update" RLS policy (auth.uid() = id).
    [Patch("/profiles")]
    Task UpdateProfileAsync([AliasAs("id")] string idEq, [Body] UpdateProfileRequest request);
}
