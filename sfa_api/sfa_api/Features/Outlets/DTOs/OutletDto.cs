namespace sfa_api.Features.Outlets.DTOs;

public record OutletDto(
    int Id,
    string Name,
    string Address,
    string Tel,
    string? Email,
    string? ContactPerson,
    string NicNo,
    string? VatNo,
    decimal CreditLimit,
    double Latitude,
    double Longitude,
    DateTime? OwnerDOB,
    string? Remarks,
    string? Image,
    string OutletType,
    string OutletCategory,
    int? ProvinceCode,
    int? DistrictCode,
    int RouteId,
    string RouteName,
    int DivisionId,
    string DivisionName,
    int TerritoryId,
    string TerritoryName,
    int AreaId,
    string AreaName,
    int RegionId,
    string RegionName,
    bool IsActive,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastBillDate
);

public record OutletListDto(
    IEnumerable<OutletDto> Outlets,
    int TotalCount,
    int Page,
    int PageSize
);

// Returned by GET /api/v1/outlets/by-route/{routeId} — wraps the outlet list with the
// calling rep's effective proximity policy so the mobile app never needs a separate
// config call.
//
// GeofenceEnforced is false while the rep holds a live proximity exemption; the app
// then stops hiding out-of-range outlets. GeofenceEnforcedFrom carries the instant
// enforcement resumes, so a cached policy expires on the device on its own instead
// of waiting for the next sync. Both are per-caller and therefore resolved outside
// the shared per-route outlet cache.
public record MobileOutletSyncDto(
    IEnumerable<OutletDto> Outlets,
    double GeofenceRadiusMeters,
    bool GeofenceEnforced = true,
    DateTime? GeofenceEnforcedFrom = null,
    string? ExemptionReason = null
);
