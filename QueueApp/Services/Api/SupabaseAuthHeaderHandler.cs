using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using QueueApp.Constants;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Api;

// Adds the Supabase apikey to every request, and the user's bearer token when signed in. With no
// session, the anon key alone is used, which works because the RLS policies permit the operations
// anonymous callers are allowed to make.
//
// The token is taken from ISessionRefreshService rather than read straight out of secure storage, so
// a session that expires while the app is running is renewed here instead of reaching the user as a
// "JWT expired" popup:
//   * before the call, if the stored token has expired or is about to;
//   * after the call, if Supabase rejects the token anyway (401) — the token is renewed and the
//     request replayed once.
// The second case covers the clock being out and a token that GoTrue revoked early.
//
// ISessionRefreshService is safe to depend on from here — unlike IAuthService it is not built on the
// Refit clients this handler backs, so there is no circular resolution in IHttpClientFactory.
public class SupabaseAuthHeaderHandler : DelegatingHandler
{
    private readonly ISessionRefreshService _sessionRefresh;

    public SupabaseAuthHeaderHandler(ISessionRefreshService sessionRefresh)
    {
        _sessionRefresh = sessionRefresh;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _sessionRefresh.GetValidAccessTokenAsync(cancellationToken);
        ApplySupabaseHeaders(request, token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized
            || string.IsNullOrEmpty(token)
            || IsTokenGrantRequest(request))
        {
            return response;
        }

        // The token looked live by our own reckoning and Supabase disagreed. One renewal and one
        // replay: if the retry is unauthorised too, that response is the honest answer.
        Debug.WriteLine($"[Auth] 401 on {request.RequestUri?.AbsolutePath} — renewing token and retrying once");

        var refreshedToken = await _sessionRefresh.RefreshAsync(token, cancellationToken);
        if (string.IsNullOrEmpty(refreshedToken) || string.Equals(refreshedToken, token, StringComparison.Ordinal))
            return response;

        var retry = await CloneRequestAsync(request, cancellationToken);
        ApplySupabaseHeaders(retry, refreshedToken);

        response.Dispose();
        return await base.SendAsync(retry, cancellationToken);
    }

    // Set rather than added: the retry starts from a copy of the original request, which already
    // carries these.
    private static void ApplySupabaseHeaders(HttpRequestMessage request, string? accessToken)
    {
        request.Headers.Remove("apikey");
        request.Headers.TryAddWithoutValidation("apikey", SupabaseConfig.AnonKey);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", string.IsNullOrEmpty(accessToken) ? SupabaseConfig.AnonKey : accessToken);

        // PostgREST: return the row(s) written so callers can read results back if needed.
        request.Headers.Remove("Prefer");
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");
    }

    // Sign-in and sign-up come back 401 for a wrong password, not an expired token — renewing and
    // replaying those would be both pointless and confusing in the logs.
    private static bool IsTokenGrantRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath;

        return path is not null
            && (path.StartsWith("/auth/v1/token", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/auth/v1/signup", StringComparison.OrdinalIgnoreCase));
    }

    // A sent request cannot be sent again — its content stream has already been read — so the replay
    // goes out on a copy.
    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(body);

            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);

            clone.Content = content;
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in (IDictionary<string, object?>)request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        return clone;
    }
}
