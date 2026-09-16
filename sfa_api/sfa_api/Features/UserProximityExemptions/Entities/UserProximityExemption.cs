using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.UserProximityExemptions.Entities;

/// <summary>
/// One admin-granted, time-bounded relief from the billing geofence for one rep.
///
/// Modelled as a grant *log* rather than a flag on <see cref="User"/> so the
/// history survives: who granted what, why, for how long, and who revoked it.
/// The effective policy is always "the latest live row covering now" — resolved
/// in one place by <c>ProximityPolicyResolver</c>, never read directly.
///
/// Rows are never hard-deleted (see .claude/rules/never-do.md). Revoking sets
/// <see cref="IsActive"/> false and stamps <see cref="RevokedAt"/>.
/// </summary>
public class UserProximityExemption
{
    public int Id { get; set; }

    /// The rep the exemption applies to.
    public int UserId { get; set; }

    /// Absolute instant the grant starts — always "now" at grant time.
    public DateTime ValidFrom { get; set; }

    /// Absolute instant the grant stops being effective (exclusive).
    /// Derived server-side from the admin's Sri Lanka business date, so a grant
    /// through 2026-09-20 expires at Colombo midnight, not UTC midnight.
    public DateTime ValidTo { get; set; }

    public ProximityExemptionReason Reason { get; set; }

    /// Free text from the admin — the specifics the reason code cannot carry.
    public string? Notes { get; set; }

    public int GrantedByUserId { get; set; }

    public DateTime? RevokedAt { get; set; }
    public int? RevokedByUserId { get; set; }

    /// False once revoked. Expiry is *not* reflected here — an expired row stays
    /// IsActive=true and simply stops matching the resolver's time window, which
    /// keeps expiry a pure function of the clock and needs no background job.
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    public uint RowVersion { get; set; }

    // Navigation
    public User? User { get; set; }
    public User? GrantedByUser { get; set; }
}
