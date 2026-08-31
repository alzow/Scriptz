using Refit;
using QueueApp.Services.Api.Auth.Models;

namespace QueueApp.Services.Api.Auth;

public interface IAuthApi
{
    [Post("/auth/v1/token?grant_type=password")]
    Task<AuthTokenResponse> SignInAsync([Body] SignInRequest request);

    [Post("/auth/v1/signup")]
    Task<AuthTokenResponse> SignUpAsync([Body] SignUpRequest request);

    [Post("/auth/v1/token?grant_type=refresh_token")]
    Task<AuthTokenResponse> RefreshTokenAsync([Body] RefreshTokenRequest request);

    [Post("/rest/v1/rpc/is_phone_available")]
    Task<bool> IsPhoneAvailableAsync([Body] PhoneCheckRequest request);

    [Get("/auth/v1/user")]
    Task<AuthUser> GetUserAsync();

    // TODO: delete_my_account doesn't exist server-side yet. profiles has no self-DELETE RLS
    // policy and profiles.id FKs to auth.users, so the actual deletion needs a Postgres function
    // (running as the service role) or an Edge Function — an anon-key client can't remove an
    // auth.users row on its own. Needs a migration decision too: hard-delete vs. anonymise, since
    // shops keep their own visit/booking records independent of the customer's account.
    [Post("/rest/v1/rpc/delete_my_account")]
    Task DeleteMyAccountAsync();
}
