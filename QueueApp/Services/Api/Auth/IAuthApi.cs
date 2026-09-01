using Refit;
using QueueApp.Services.Api.Auth.Models;

namespace QueueApp.Services.Api.Auth;

public interface IAuthApi
{
    [Post("/auth/v1/token?grant_type=password")]
    Task<AuthTokenResponse> SignInAsync([Body] SignInRequest request);

    [Post("/auth/v1/signup")]
    Task<AuthTokenResponse> SignUpAsync([Body] SignUpRequest request);

    // Renewing a token lives on ITokenRefreshApi, not here: this client's pipeline is the one that
    // renews expired tokens, so a refresh sent through it would be a cycle.

    [Post("/rest/v1/rpc/is_phone_available")]
    Task<bool> IsPhoneAvailableAsync([Body] PhoneCheckRequest request);

    [Get("/auth/v1/user")]
    Task<AuthUser> GetUserAsync();
}
