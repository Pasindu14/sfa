namespace sfa_api.Features.UserProximityExemptions.Entities;

/// <summary>
/// Why a rep was granted relief from the billing geofence. Stored as the enum
/// member name (string) so the value stays readable in the database and in
/// exports; see AppDbContext for the length-bounded column mapping.
///
/// Reason codes exist so the grants are reportable later — "how often is
/// BadOutletCoordinates the cause?" is the question that tells you to go fix
/// outlet coordinates instead of handing out more exemptions.
/// </summary>
public enum ProximityExemptionReason
{
    /// The outlet's stored coordinates are wrong, so the rep is genuinely on site
    /// but measures as far away. The real fix is correcting the outlet position.
    BadOutletCoordinates = 1,

    /// Many outlets share a single coordinate (a wholesale market, a multi-floor
    /// building), so a per-outlet radius cannot separate them.
    SharedCoordinateMarket = 2,

    /// The rep's handset reports a bad or drifting fix.
    DeviceGpsFault = 3,

    /// Deliberate business exception signed off by management.
    ManagementApproval = 4
}
