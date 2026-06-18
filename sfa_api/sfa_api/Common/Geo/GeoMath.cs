namespace sfa_api.Common.Geo;

public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000;

    /// Returns the great-circle distance in metres between two WGS-84 points.
    /// Returns double.MaxValue when either point is (0, 0) — treated as
    /// "no coordinate stored", so the caller can skip the proximity check.
    public static double HaversineMeters(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        if ((lat1 == 0 && lon1 == 0) || (lat2 == 0 && lon2 == 0))
            return double.MaxValue;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}
