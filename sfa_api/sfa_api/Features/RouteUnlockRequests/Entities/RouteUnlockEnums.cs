namespace sfa_api.Features.RouteUnlockRequests.Entities;

/// <summary>
/// Stored lifecycle state. "Expired" is deliberately not a member: it is a pure
/// function of the clock (a Pending row from a past day, or an Approved row past
/// ValidTo), so it is computed on read and needs no background job.
/// </summary>
public enum RouteUnlockStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Revoked
}

public enum RouteUnlockAction
{
    Requested,
    Approved,
    Rejected,
    Cancelled,
    Revoked
}
