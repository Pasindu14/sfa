namespace sfa_api.Features.UserGeoAssignments.DTOs;

public record UserAssignmentDto(
    int Id,
    int UserId,
    string UserName,
    string UserRole,
    int? ReportsToUserId,
    string? ReportsToUserName,
    int? DivisionId,
    string? DivisionName,
    int? TerritoryId,
    string? TerritoryName,
    int? AreaId,
    string? AreaName,
    int? RegionId,
    string? RegionName,
    DateOnly EffectiveFrom,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UserAssignmentListDto(
    IEnumerable<UserAssignmentDto> UserAssignments,
    int TotalCount,
    int Page,
    int PageSize
);

public record UserAssignmentStatsDto(
    int TotalAssignments,
    int ActiveAssignments,
    int ActiveTerritories,
    int AssignmentsThisMonth
);
