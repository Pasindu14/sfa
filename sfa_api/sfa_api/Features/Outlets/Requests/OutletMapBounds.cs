using sfa_api.Common.Errors;

namespace sfa_api.Features.Outlets.Requests;

/// <summary>
/// Optional viewport bounding box for <c>GET /api/v1/outlets/map-points</c>
/// (query params <c>minLat</c>, <c>minLng</c>, <c>maxLat</c>, <c>maxLng</c>; bounds inclusive).
/// </summary>
public sealed record OutletMapBounds(double MinLat, double MinLng, double MaxLat, double MaxLng)
{
    /// <summary>
    /// None supplied → <c>null</c> (unfiltered, the original response). All four supplied and valid →
    /// the bounds. Anything else (partial set, non-finite, lat outside -90..90, lng outside -180..180,
    /// min &gt; max) → <see cref="ValidationException"/> with per-field errors (400 VALIDATION_FAILED).
    /// </summary>
    public static OutletMapBounds? FromQuery(double? minLat, double? minLng, double? maxLat, double? maxLng)
    {
        var values = new (string Name, double? Value, double Limit)[]
        {
            ("minLat", minLat, 90), ("minLng", minLng, 180), ("maxLat", maxLat, 90), ("maxLng", maxLng, 180),
        };

        if (values.All(v => v.Value is null)) return null;

        var errors = new Dictionary<string, string[]>();

        if (values.Any(v => v.Value is null))
        {
            foreach (var (name, value, _) in values)
                if (value is null)
                    errors[name] = [$"{name} is required when any of minLat, minLng, maxLat, maxLng is provided."];
            throw new ValidationException(errors);
        }

        foreach (var (name, value, limit) in values)
        {
            var v = value!.Value;
            if (!double.IsFinite(v) || v < -limit || v > limit)
                errors[name] = [$"{name} must be between {-limit} and {limit}."];
        }

        if (!errors.ContainsKey("minLat") && !errors.ContainsKey("maxLat") && minLat > maxLat)
            errors["minLat"] = ["minLat must be less than or equal to maxLat."];
        if (!errors.ContainsKey("minLng") && !errors.ContainsKey("maxLng") && minLng > maxLng)
            errors["minLng"] = ["minLng must be less than or equal to maxLng."];

        if (errors.Count > 0) throw new ValidationException(errors);

        return new OutletMapBounds(minLat!.Value, minLng!.Value, maxLat!.Value, maxLng!.Value);
    }
}
