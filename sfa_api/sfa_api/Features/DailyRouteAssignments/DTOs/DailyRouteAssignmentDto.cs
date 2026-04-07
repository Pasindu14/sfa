using sfa_api.Features.DailyRouteAssignments.Enums;

namespace sfa_api.Features.DailyRouteAssignments.DTOs;

public record DailyRouteAssignmentDto(
    int Id,
    int UserId,
    string UserName,
    int RouteId,
    string RouteName,
    DateOnly AssignedDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DailyRouteAssignmentDeletionStatus DeletionStatus,
    DateTime? DeletionRequestedAt,
    string? DeletionRequestReason,
    string? DeletionRejectionReason
);

public record DailyRouteAssignmentListDto(
    IEnumerable<DailyRouteAssignmentDto> Assignments,
    int TotalCount,
    int Page,
    int PageSize
);

public record RepSummaryDto(
    int UserId,
    string UserName
);

public record RepRouteDto(
    int RouteId,
    string RouteName
);

public record RequestDeletionBody(string? Reason);

public record RejectDeletionRequest(string? Reason);
