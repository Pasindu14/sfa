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
// server-configured proximity radius so the mobile app never needs a separate config call.
public record MobileOutletSyncDto(
    IEnumerable<OutletDto> Outlets,
    double GeofenceRadiusMeters
);
