namespace sfa_api.Features.Billings.Options;

public class BillingGeoOptions
{
    public bool EnforceProximity { get; set; } = true;

    /// Maximum allowed distance (metres) between rep and outlet.
    public double RadiusMeters { get; set; } = 1000.0;

    /// Extra tolerance on top of RadiusMeters to absorb GPS jitter and
    /// outlet-coordinate inaccuracy. Server enforces RadiusMeters + ToleranceMeters.
    public double ToleranceMeters { get; set; } = 200.0;
}
