using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.RouteUnlockRequests.Entities;

/// <summary>
/// Append-only timeline entry: one row per transition of a
/// <see cref="RouteUnlockRequest"/>. Never updated or deleted — this is the
/// permanent "who did what, when, from where" record.
/// </summary>
public class RouteUnlockRequestEvent
{
    public long Id { get; set; }
    public int RouteUnlockRequestId { get; set; }

    public RouteUnlockAction Action { get; set; }
    public RouteUnlockStatus? FromStatus { get; set; }
    public RouteUnlockStatus ToStatus { get; set; }

    public int PerformedByUserId { get; set; }
    /// Role snapshot — a user's role can change later; the record must not.
    public string PerformedByRole { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? Note { get; set; }

    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }

    public RouteUnlockRequest? Request { get; set; }
    public User? PerformedByUser { get; set; }
}
