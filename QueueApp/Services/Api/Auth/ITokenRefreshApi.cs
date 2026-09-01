using Refit;
using QueueApp.Services.Api.Auth.Models;

namespace QueueApp.Services.Api.Auth;

// The refresh call gets its own Refit client so the token pipeline has no cycle in it:
// SupabaseAuthHeaderHandler needs something that can renew an expired token, and a renewer built on
// IAuthApi would need that same handler back to make its call. This client goes out through
// SupabaseAnonKeyHandler instead — anon key only, no stored bearer token — so a refresh can never
// recurse into another refresh.
public interface ITokenRefreshApi
{
    [Post("/auth/v1/token?grant_type=refresh_token")]
    Task<AuthTokenResponse> RefreshTokenAsync([Body] RefreshTokenRequest request);
}
