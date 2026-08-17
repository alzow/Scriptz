using System.Net.Http.Headers;
using ScriptzApp.Constants;
using ScriptzApp.Services.Auth;

namespace ScriptzApp.Services.Api;

// Adds the Supabase apikey to every request, and the user's bearer token when signed in.
// Before real auth is wired up, the anon key alone is used, which works because
// Step 1's RLS policies permit the needed operations for anonymous callers.
public class SupabaseAuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public SupabaseAuthHeaderHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("apikey", SupabaseConfig.AnonKey);

        var token = await _authService.GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", string.IsNullOrEmpty(token) ? SupabaseConfig.AnonKey : token);

        // PostgREST: return the row(s) written so callers can read results back if needed.
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

        return await base.SendAsync(request, cancellationToken);
    }
}
