namespace QueueApp.Services.Auth;

// Owns the stored access/refresh token pair and is the only thing that renews it. Everything that
// needs a token goes through GetValidAccessTokenAsync, so a session that expires while the app is
// running is renewed in place instead of surfacing as a "JWT expired" error on the next call.
public interface ISessionRefreshService
{
    // Raised when the session is gone for good — no refresh token, or GoTrue rejected the one we
    // had — after the stored tokens have been cleared. A network failure is not this: the session
    // may still be perfectly good, so it is left alone and the caller's own request fails instead.
    event EventHandler? SessionExpired;

    // Raised with the new access token after a successful renewal, for anything holding a
    // connection that was authorised with the old one (the realtime socket).
    event EventHandler<string>? SessionRefreshed;

    // The current access token, renewed first if it has expired or is about to. Null when there is
    // no session left — including one a refused renewal just cleared. A renewal that could not be
    // made at all (offline) hands back the token already held rather than nothing: it may still be
    // good, and the request is a better place to fail than here.
    Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);

    // Forces a renewal of the token the caller was using. Concurrent callers coalesce onto one
    // refresh: whoever gets there first does the call, the rest are handed the resulting token.
    Task<string?> RefreshAsync(string? staleAccessToken, CancellationToken cancellationToken = default);

    Task StoreSessionAsync(string accessToken, string? refreshToken, int expiresInSeconds);

    Task ClearSessionAsync();
}
