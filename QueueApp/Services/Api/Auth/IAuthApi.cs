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
}
