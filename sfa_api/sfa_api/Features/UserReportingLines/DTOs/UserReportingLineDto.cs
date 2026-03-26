namespace sfa_api.Features.UserReportingLines.DTOs;

public record UserReportingLineDto(
    int Id,
    int UserId,
    string UserName,
    string UserRole,
    int ReportsToUserId,
    string ReportsToUserName,
    string ReportsToUserRole,
    DateOnly EffectiveFrom,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UserReportingLineListDto(
    IEnumerable<UserReportingLineDto> UserReportingLines,
    int TotalCount,
    int Page,
    int PageSize
);
