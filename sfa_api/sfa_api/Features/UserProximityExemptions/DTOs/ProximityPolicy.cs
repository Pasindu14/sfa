using sfa_api.Features.UserProximityExemptions.Entities;

namespace sfa_api.Features.UserProximityExemptions.DTOs;

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
/// When enforcement resumes — the exemption's ValidTo. Sent to the client so a
/// cached policy expires on its own instead of waiting for the next sync.
/// Null when enforcement is already on, or when it is off globally by config.
/// </param>
/// <param name="ExemptionId">The grant that relaxed the policy, for audit stamping.</param>
/// <param name="Reason">Why it was relaxed.</param>
public readonly record struct ProximityPolicy(
    bool Enforced,
    double RadiusMeters,
    double ToleranceMeters,
    DateTime? EnforcedFrom,
    int? ExemptionId,
    ProximityExemptionReason? Reason
)
{
    /// The distance beyond which a bill is refused. Only meaningful when Enforced.
    public double LimitMeters => RadiusMeters + ToleranceMeters;
}
