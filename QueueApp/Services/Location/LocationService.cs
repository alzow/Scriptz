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

        if (!double.TryParse(latRaw, out var lat) || !double.TryParse(lngRaw, out var lng))
            return null;

        return new CustomerLocation(lat, lng, string.IsNullOrWhiteSpace(label) ? "Current location" : label);
    }

    public async Task<CustomerLocation?> RefreshLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return null;

            using var cts = new CancellationTokenSource(FixTimeout);
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, FixTimeout);
            var position = await Geolocation.Default.GetLocationAsync(request, cts.Token);
            if (position is null)
                return null;

            var label = await ReverseGeocodeAsync(position.Latitude, position.Longitude);

            await _secureStorage.SetAsync(LatitudeKey, position.Latitude.ToString("R"));
            await _secureStorage.SetAsync(LongitudeKey, position.Longitude.ToString("R"));
            await _secureStorage.SetAsync(LabelKey, label);

            return new CustomerLocation(position.Latitude, position.Longitude, label);
        }
        catch (Exception ex)
        {
            // Permission denied, location services off, no fix within the timeout, unsupported
            // platform (e.g. some simulators) — all non-fatal, caller falls back to suburb-only.
            System.Diagnostics.Debug.WriteLine($"[Location] refresh failed: {ex.Message}");
            return null;
        }
    }

    private static async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(latitude, longitude);
            var placemark = placemarks?.FirstOrDefault();
            return placemark?.Locality
                ?? placemark?.SubLocality
                ?? placemark?.AdminArea
                ?? "Current location";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Location] reverse geocode failed: {ex.Message}");
            return "Current location";
        }
    }
}
