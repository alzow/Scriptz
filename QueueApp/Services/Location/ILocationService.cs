namespace QueueApp.Services.Location;

// Latitude/longitude plus a short human-readable label (suburb/locality) for display —
// e.g. in the Browse dashboard's location bar.
public record CustomerLocation(double Latitude, double Longitude, string Label);

public interface ILocationService
{
    // Last resolved location, read from local cache only (no GPS fix, no permission prompt) —
    // for an instant first paint before RefreshLocationAsync gets a live fix.
    Task<CustomerLocation?> GetCachedLocationAsync();

    // Requests permission if needed, gets a fresh GPS fix, reverse-geocodes it to a label, and
    // caches the result. Returns null (never throws) if permission is denied, location services
    // are off, or the fix times out — callers should fall back to suburb-only browsing.
    Task<CustomerLocation?> RefreshLocationAsync();
}
