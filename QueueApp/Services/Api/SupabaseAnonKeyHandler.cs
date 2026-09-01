using System.Net.Http.Headers;
using QueueApp.Constants;

namespace QueueApp.Services.Api;

// The unauthenticated half of the pipeline: apikey plus the anon key as bearer, and nothing that
// reads or renews the stored session. ITokenRefreshApi is sent through this rather than
// SupabaseAuthHeaderHandler, which is what keeps a token refresh from triggering a token refresh.
public class SupabaseAnonKeyHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("apikey", SupabaseConfig.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SupabaseConfig.AnonKey);

        return base.SendAsync(request, cancellationToken);
    }
}
