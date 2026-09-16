namespace sfa_api.Features.UserProximityExemptions.Requests;

public class GrantProximityExemptionRequest
{
    /// Sri Lanka business date the exemption runs through, inclusive (yyyy-MM-dd).
    public DateOnly ValidUntil { get; set; }

    /// One of the ProximityExemptionReason member names.
    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class RevokeProximityExemptionRequest
{
    /// xmin of the grant being revoked — guards against two admins racing.
    public uint RowVersion { get; set; }
}
