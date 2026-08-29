namespace QueueApp.Services.Location;

// Latitude/longitude plus a short human-readable label (suburb/locality) for display —
// e.g. in the Browse dashboard's location bar.
public record CustomerLocation(double Latitude, double Longitude, string Label)
{
    // Two fixes of a stationary phone are never bit-identical, so "has the customer moved" has to
    // be a distance question. Comparing the raw doubles made every fix a move, which re-fetched the
    // whole browse list on every dashboard open.
    private const double MeaningfulMoveMetres = 250;

    private const double EarthRadiusMetres = 6_371_000;

    public bool HasMovedFrom(double? latitude, double? longitude)
    {
        if (latitude is not { } lat || longitude is not { } lon)
            return true;

        return DistanceMetres(lat, lon, Latitude, Longitude) > MeaningfulMoveMetres;
    }

    private static double DistanceMetres(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return EarthRadiusMetres * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

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
