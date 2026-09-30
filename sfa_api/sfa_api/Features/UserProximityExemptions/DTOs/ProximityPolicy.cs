using sfa_api.Features.UserProximityExemptions.Entities;

namespace sfa_api.Features.UserProximityExemptions.DTOs;

/// <summary>What relaxed the policy, when something did.</summary>
public enum ProximityPolicySource
{
    None,
    /// An admin-granted <c>UserProximityExemption</c> — rep-wide, any route.
    AdminExemption,
    /// An approved <c>RouteUnlockRequest</c> — one route, one business day.
    RouteUnlock
}

/// <summary>
/// The effective geofence policy for one rep at one instant. Produced only by
/// <c>IProximityPolicyResolver</c> and consumed by both the mobile outlet sync
/// (so the app can stop hiding distant outlets) and the bill-create gate (so the
/// server actually lets the bill through). One resolver, two callers — if the
/// read side and the write side computed this separately they would drift.
/// </summary>
/// <param name="Enforced">False when the rep may bill at any distance.</param>
/// <param name="RadiusMeters">The radius the client should draw its circle at.</param>
/// <param name="ToleranceMeters">Server-side slack on top of the radius; not published to clients.</param>
/// <param name="EnforcedFrom">
/// When enforcement resumes — the exemption's or unlock's ValidTo. Sent to the
/// client so a cached policy expires on its own instead of waiting for the next
/// sync. Null when enforcement is already on, or when it is off globally by config.
/// </param>
/// <param name="ExemptionId">The admin grant that relaxed the policy, for audit stamping.</param>
/// <param name="Reason">Why the admin grant relaxed it.</param>
/// <param name="Source">Which mechanism relaxed it.</param>
/// <param name="RouteUnlockRequestId">The approved route unlock that relaxed it, for audit stamping.</param>
public readonly record struct ProximityPolicy(
    bool Enforced,
    double RadiusMeters,
    double ToleranceMeters,
    DateTime? EnforcedFrom,
    int? ExemptionId,
    ProximityExemptionReason? Reason,
    ProximityPolicySource Source = ProximityPolicySource.None,
    int? RouteUnlockRequestId = null
)
{
    /// The distance beyond which a bill is refused. Only meaningful when Enforced.
    public double LimitMeters => RadiusMeters + ToleranceMeters;

    /// The value published to the app as <c>exemptionReason</c>.
    public string? ExemptionReasonLabel => Source switch
    {
        ProximityPolicySource.RouteUnlock => "RouteUnlock",
        ProximityPolicySource.AdminExemption => Reason?.ToString(),
        _ => null
    };
}
