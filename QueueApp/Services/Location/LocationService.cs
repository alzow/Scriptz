using QueueApp.Services.Storage;

namespace QueueApp.Services.Location;

// Wraps device GPS + reverse geocoding. Deliberately doesn't persist location server-side —
// each nearby_business_summary request carries it fresh (see NearbyBusinessSummaryRequest) —
// only this local cache exists, so it lives and dies with the device, never Supabase.
public class LocationService : ILocationService
{
    private const string LatitudeKey = "loc_last_latitude";
    private const string LongitudeKey = "loc_last_longitude";
    private const string LabelKey = "loc_last_label";
    private const string IsCoarseKey = "loc_last_is_coarse";
    private const string ResolvedAtKey = "loc_last_resolved_at";

    private static readonly TimeSpan FixTimeout = TimeSpan.FromSeconds(12);

    private readonly ISecureStorageService _secureStorage;

    public LocationService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<CustomerLocation?> GetCachedLocationAsync()
    {
        var latRaw = await _secureStorage.GetAsync(LatitudeKey);
        var lngRaw = await _secureStorage.GetAsync(LongitudeKey);
        var label = await _secureStorage.GetAsync(LabelKey);
        var isCoarseRaw = await _secureStorage.GetAsync(IsCoarseKey);
        var resolvedAtRaw = await _secureStorage.GetAsync(ResolvedAtKey);

        if (!double.TryParse(latRaw, out var lat) || !double.TryParse(lngRaw, out var lng))
            return null;

        var isCoarse = bool.TryParse(isCoarseRaw, out var coarse) && coarse;
        var resolvedAt = DateTimeOffset.TryParse(resolvedAtRaw, out var parsed) ? parsed : DateTimeOffset.MinValue;

        return new CustomerLocation(
            lat, lng, string.IsNullOrWhiteSpace(label) ? "Current location" : label, isCoarse, resolvedAt);
    }

    public async Task<LocationResolution> RefreshLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return LocationResolution.Denied;

            using var cts = new CancellationTokenSource(FixTimeout);
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, FixTimeout);
            var position = await Geolocation.Default.GetLocationAsync(request, cts.Token);
            if (position is null)
                return LocationResolution.Failed;

            var (label, isCoarse) = await ReverseGeocodeAsync(position.Latitude, position.Longitude);
            var resolvedAt = DateTimeOffset.UtcNow;

            await _secureStorage.SetAsync(LatitudeKey, position.Latitude.ToString("R"));
            await _secureStorage.SetAsync(LongitudeKey, position.Longitude.ToString("R"));
            await _secureStorage.SetAsync(LabelKey, label);
            await _secureStorage.SetAsync(IsCoarseKey, isCoarse.ToString());
            await _secureStorage.SetAsync(ResolvedAtKey, resolvedAt.ToString("O"));

            var location = new CustomerLocation(position.Latitude, position.Longitude, label, isCoarse, resolvedAt);
            return new LocationResolution(isCoarse ? LocationOutcome.Coarse : LocationOutcome.Resolved, location);
        }
        catch (Exception ex)
        {
            // Location services off, no fix within the timeout, unsupported platform (e.g. some
            // simulators) — all non-fatal, caller falls back to suburb-only browsing.
            System.Diagnostics.Debug.WriteLine($"[Location] refresh failed: {ex.Message}");
            return LocationResolution.Failed;
        }
    }

    // Street-level when the placemark has one (Resolved); suburb-level, or a geocode that failed
    // outright, both read as Coarse — the bar shows "Lenasia" either way rather than pretending a
    // suburb match is a street address.
    private static async Task<(string Label, bool IsCoarse)> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(latitude, longitude);
            var placemark = placemarks?.FirstOrDefault();
            if (placemark is null)
                return ("Current location", true);

            var suburb = placemark.Locality ?? placemark.SubLocality ?? placemark.AdminArea;

            if (!string.IsNullOrWhiteSpace(placemark.Thoroughfare) && !string.IsNullOrWhiteSpace(suburb))
            {
                var street = string.IsNullOrWhiteSpace(placemark.SubThoroughfare)
                    ? placemark.Thoroughfare
                    : $"{placemark.SubThoroughfare} {placemark.Thoroughfare}";
                return ($"{street}, {suburb}", false);
            }

            return (string.IsNullOrWhiteSpace(suburb) ? "Current location" : suburb, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Location] reverse geocode failed: {ex.Message}");
            return ("Current location", true);
        }
    }
}
