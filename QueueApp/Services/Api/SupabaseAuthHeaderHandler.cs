using System.Net.Http.Headers;
using ScriptzApp.Constants;
using ScriptzApp.Services.Storage;

namespace ScriptzApp.Services.Api;

// Adds the Supabase apikey to every request, and the user's bearer token when signed in.
// Before real auth is wired up, the anon key alone is used, which works because
// Step 1's RLS policies permit the needed operations for anonymous callers.
//
// Reads the token directly from ISecureStorageService rather than IAuthService: this handler
// backs the IAuthApi Refit client itself, so depending on IAuthService (which depends on
// IAuthApi) would create a circular resolution in IHttpClientFactory.
public class SupabaseAuthHeaderHandler : DelegatingHandler
{
    private readonly ISecureStorageService _secureStorage;

    public SupabaseAuthHeaderHandler(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("apikey", SupabaseConfig.AnonKey);

        var token = await _secureStorage.GetAsync(SupabaseConfig.AccessTokenKey);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", string.IsNullOrEmpty(token) ? SupabaseConfig.AnonKey : token);

        // PostgREST: return the row(s) written so callers can read results back if needed.
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

        return await base.SendAsync(request, cancellationToken);
    }
}
