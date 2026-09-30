using sfa_api.Features.Users.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.RouteUnlockRequests.Entities;

/// <summary>
/// A rep's request to lift the billing geofence for every outlet on their
/// assigned route for one Sri Lanka business day, approved by their supervisor
/// or an admin.
///
/// Unlike an admin <c>UserProximityExemption</c> this is scoped to one route and
/// one day. The effective policy is still decided only by
/// <c>ProximityPolicyResolver</c>; nothing else reads this table for enforcement.
///
/// Rows are never deleted. Every transition also appends a
/// <see cref="RouteUnlockRequestEvent"/>, which is the permanent audit trail
/// (the generic AuditLog is purged after 90 days).
/// </summary>
public class RouteUnlockRequest
{
    public int Id { get; set; }

    // What was asked for — resolved server-side from today's assignment, never
    // taken from the client.
    public int UserId { get; set; }
    public int RouteId { get; set; }
    public int DailyRouteAssignmentId { get; set; }
    public DateOnly BusinessDate { get; set; }

    public RouteUnlockStatus Status { get; set; } = RouteUnlockStatus.Pending;

    // Request context
    public string RequestReason { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public double? RequestLatitude { get; set; }
    public double? RequestLongitude { get; set; }
    public double? RequestGpsAccuracyMeters { get; set; }

    /// Snapshot of the rep's direct manager at request time — who it was routed
    /// to. Null when the rep had no reporting line (admins only can act).
    public int? SupervisorUserId { get; set; }

    // Review (approve or reject)
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByRole { get; set; }
    public DateTime? ReviewedAt { get; set; }
    /// The approver's note, or the rejection reason.
    public string? ReviewNote { get; set; }

    /// Effective window, set on approval: [approval instant, Colombo midnight).
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    // Revoke / cancel
    public int? RevokedByUserId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public DateTime? CancelledAt { get; set; }

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
    public RouteEntity? Route { get; set; }
    public User? SupervisorUser { get; set; }
    public User? ReviewedByUser { get; set; }
    public User? RevokedByUser { get; set; }
    public List<RouteUnlockRequestEvent> Events { get; set; } = [];
}
