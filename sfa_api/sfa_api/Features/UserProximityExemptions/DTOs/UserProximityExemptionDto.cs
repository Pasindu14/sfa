namespace sfa_api.Features.UserProximityExemptions.DTOs;

public record UserProximityExemptionDto(
    int Id,
    int UserId,
    string Name,
    string Username,
    DateTime ValidFrom,
    DateTime ValidTo,
    /// The Sri Lanka business date the grant runs through (inclusive) — what the
    /// admin picked, echoed back so the UI need not undo the UTC conversion.
    DateOnly ValidUntilDate,
    string Reason,
    string? Notes,
    int GrantedByUserId,
    string? GrantedByUserName,
    DateTime? RevokedAt,
    int? RevokedByUserId,
    bool IsActive,
    /// True when this grant is the one in force right now.
    bool IsCurrentlyEffective,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UserProximityExemptionListDto(
    IEnumerable<UserProximityExemptionDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// The rep-facing view of their own policy, surfaced on the outlet sync so the
/// app can explain itself ("location check off until 20 Sep") rather than
/// silently behaving differently.
/// </summary>
public record ProximityPolicyDto(
    bool GeofenceEnforced,
    double GeofenceRadiusMeters,
    DateTime? GeofenceEnforcedFrom,
    string? ExemptionReason
);
