namespace sfa_api.Features.RouteUnlockRequests.Requests;

public class CreateRouteUnlockRequest
{
    public string Reason { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? GpsAccuracyMeters { get; set; }
}

public class ApproveRouteUnlockRequest
{
    public uint RowVersion { get; set; }
    public string? Note { get; set; }
}

/// Used by reject and revoke — both need a reason on the record.
public class ReasonedRouteUnlockRequest
{
    public uint RowVersion { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CancelRouteUnlockRequest
{
    public uint RowVersion { get; set; }
}

public class RouteUnlockListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    /// An effectiveStatus value: Pending, Approved, Expired, Rejected, Cancelled, Revoked.
    public string? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
