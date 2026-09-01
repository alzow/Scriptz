using System.Diagnostics;
using System.Globalization;
using QueueApp.Constants;
using QueueApp.Services.Api.Auth;
using QueueApp.Services.Api.Auth.Models;
using QueueApp.Services.Storage;
using Refit;

namespace QueueApp.Services.Auth;

public class SessionRefreshService : ISessionRefreshService
{
    // Renew a little before the token actually dies, so a call that takes a moment to reach
    // Supabase isn't authorised with a token that expires in flight.
    private static readonly TimeSpan ExpiryLeeway = TimeSpan.FromSeconds(60);

    private readonly ISecureStorageService _secureStorage;
    private readonly ITokenRefreshApi _refreshApi;

    // One refresh at a time. A screen that fires several calls at once would otherwise send several
    // refreshes with the same refresh token, and GoTrue rotates that token on every use — the losers
    // of the race would end up storing one that has already been rotated away.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public event EventHandler? SessionExpired;
    public event EventHandler<string>? SessionRefreshed;

    public SessionRefreshService(ISecureStorageService secureStorage, ITokenRefreshApi refreshApi)
    {
        _secureStorage = secureStorage;
        _refreshApi = refreshApi;
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await _secureStorage.GetAsync(SupabaseConfig.AccessTokenKey);
        if (string.IsNullOrEmpty(token))
            return null;

        if (!await IsExpiringAsync())
            return token;

        var refreshed = await RefreshAsync(token, cancellationToken);
        if (!string.IsNullOrEmpty(refreshed))
            return refreshed;

        // The renewal could not be made (offline, or it timed out) rather than being refused, so the
        // token we have is still the best one available — better to try the call with it than to fall
        // back to an anonymous one. Re-read, because a refused renewal will have cleared it.
        return await _secureStorage.GetAsync(SupabaseConfig.AccessTokenKey);
    }

    public async Task<string?> RefreshAsync(string? staleAccessToken, CancellationToken cancellationToken = default)
    {
        RefreshOutcome outcome;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            outcome = await RefreshCoreAsync(staleAccessToken);
        }
        finally
        {
            _gate.Release();
        }

        // Raised with the gate released: a subscriber that calls back in here would otherwise
        // deadlock against the refresh it is reacting to.
        if (outcome.Renewed && outcome.Token is not null)
            SessionRefreshed?.Invoke(this, outcome.Token);

        if (outcome.Expired)
            SessionExpired?.Invoke(this, EventArgs.Empty);

        return outcome.Token;
    }

    public async Task StoreSessionAsync(string accessToken, string? refreshToken, int expiresInSeconds)
    {
        await _secureStorage.SetAsync(SupabaseConfig.AccessTokenKey, accessToken);

        if (!string.IsNullOrEmpty(refreshToken))
            await _secureStorage.SetAsync(SupabaseConfig.RefreshTokenKey, refreshToken);

        var expiryUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        await _secureStorage.SetAsync(SupabaseConfig.TokenExpiryKey, expiryUtc.ToString("O"));
    }

    public Task ClearSessionAsync() => ClearTokensAsync();

    private readonly record struct RefreshOutcome(string? Token, bool Renewed, bool Expired);

    private async Task<RefreshOutcome> RefreshCoreAsync(string? staleAccessToken)
    {
        // Someone else may have refreshed while this call waited on the gate — if what is stored now
        // is a different, healthy token, that is the answer and there is nothing left to do.
        var current = await _secureStorage.GetAsync(SupabaseConfig.AccessTokenKey);
        if (!string.IsNullOrEmpty(current)
            && !string.Equals(current, staleAccessToken, StringComparison.Ordinal)
            && !await IsExpiringAsync())
        {
            return new RefreshOutcome(current, Renewed: false, Expired: false);
        }

        var refreshToken = await _secureStorage.GetAsync(SupabaseConfig.RefreshTokenKey);
        if (string.IsNullOrEmpty(refreshToken))
        {
            await ClearTokensAsync();
            return new RefreshOutcome(null, Renewed: false, Expired: true);
        }

        try
        {
            var response = await _refreshApi.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });

            if (string.IsNullOrEmpty(response.AccessToken))
            {
                Debug.WriteLine("[Auth] token refresh returned no access token");
                await ClearTokensAsync();
                return new RefreshOutcome(null, Renewed: false, Expired: true);
            }

            await StoreSessionAsync(response.AccessToken, response.RefreshToken, response.ExpiresIn);
            Debug.WriteLine("[Auth] access token renewed");
            return new RefreshOutcome(response.AccessToken, Renewed: true, Expired: false);
        }
        catch (ApiException ex)
        {
            // GoTrue rejected the refresh token itself — revoked, already rotated, or past its own
            // expiry. There is nothing left to renew, so the session is genuinely over.
            Debug.WriteLine($"[Auth] token refresh rejected: {ex.StatusCode} {ex.Content}");
            await ClearTokensAsync();
            return new RefreshOutcome(null, Renewed: false, Expired: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            // Offline, or the call timed out. The session may well still be valid, so it is kept and
            // the caller's own request is left to fail — signing the user out on a flaky connection
            // would be worse than the error they were going to see anyway.
            Debug.WriteLine($"[Auth] token refresh could not be completed: {ex.Message}");
            return new RefreshOutcome(null, Renewed: false, Expired: false);
        }
    }

    private async Task ClearTokensAsync()
    {
        await _secureStorage.RemoveAsync(SupabaseConfig.AccessTokenKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.RefreshTokenKey);
        await _secureStorage.RemoveAsync(SupabaseConfig.TokenExpiryKey);
    }

    // No stored expiry is treated as "renew now": the token is of unknown age, and a refresh is
    // cheaper than an authorised call that comes back 401.
    private async Task<bool> IsExpiringAsync()
    {
        var expiryRaw = await _secureStorage.GetAsync(SupabaseConfig.TokenExpiryKey);

        if (!DateTime.TryParse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiryUtc))
            return true;

        return expiryUtc.ToUniversalTime() <= DateTime.UtcNow.Add(ExpiryLeeway);
    }
}
