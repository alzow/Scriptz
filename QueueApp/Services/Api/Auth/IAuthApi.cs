using Refit;
using ScriptzApp.Services.Api.Auth.Models;

namespace ScriptzApp.Services.Api.Auth;

// Supabase Auth (GoTrue) lives at /auth/v1 off the project root.
public interface IAuthApi
{
    [Post("/auth/v1/token?grant_type=password")]
    Task<AuthTokenResponse> SignInAsync([Body] SignInRequest request);

    [Post("/auth/v1/signup")]
    Task<AuthTokenResponse> SignUpAsync([Body] SignUpRequest request);
}
