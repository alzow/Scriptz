namespace QueueApp.Shared.Domain;

// How far the customer is from a shop, and roughly how long that takes. The distance is real
// arithmetic; the travel time is an average town speed and nothing more, which is why it is only
// ever shown as an approximation and never turned into a timestamp of its own.
public static class GeoDistance
{
    private const double EarthRadiusKm = 6371;
    private const double TownKmPerHour = 24;
    private const int MinimumTravelMinutes = 3;

    public static double Kilometres(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static string Describe(double kilometres) => kilometres < 1
        ? $"{kilometres * 1000:0} m"
        : $"{kilometres:0.#} km";

    public static int TravelMinutes(double kilometres) =>
        Math.Max(MinimumTravelMinutes, (int)Math.Round(kilometres / TownKmPerHour * 60));
}
