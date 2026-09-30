namespace sfa_api.Features.RouteUnlockRequests.DTOs;

public record RouteUnlockRequestDto(
    int Id,
    int UserId,
    string UserName,
    /// The login handle. Named LoginName because "username" would collide with UserName under
    /// the case-insensitive JSON options and fail serialization of the whole response.
    string LoginName,
    int RouteId,
    string RouteName,
    DateOnly BusinessDate,
    string Status,
    /// Status plus "Expired" — a Pending row from a past day, or an Approved row past ValidTo.
    string EffectiveStatus,
    /// Approved and inside [ValidFrom, ValidTo) right now.
    bool IsCurrentlyEffective,
    string RequestReason,
    DateTime RequestedAt,
    double? RequestLatitude,
    double? RequestLongitude,
    double? RequestGpsAccuracyMeters,
    int? SupervisorUserId,
    string? SupervisorName,
    int? ReviewedByUserId,
    string? ReviewedByName,
    string? ReviewedByRole,
    DateTime? ReviewedAt,
    string? ReviewNote,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int? RevokedByUserId,
    string? RevokedByName,
    DateTime? RevokedAt,
    string? RevokeReason,
    DateTime? CancelledAt,
    uint RowVersion
);

public record RouteUnlockRequestEventDto(
    long Id,
    string Action,
    string? FromStatus,
    string ToStatus,
    int PerformedByUserId,
    string? PerformedByName,
    string PerformedByRole,
    DateTime PerformedAt,
    string? Note,
    string? IpAddress
);

public record RouteUnlockBillDto(
    int BillingId,
    string BillingNumber,
    DateOnly BillingDate,
    int OutletId,
    string OutletName,
    double? DistanceFromOutletMeters,
    decimal TotalAmount,
    DateTime CreatedAt
);

public record RouteUnlockRequestDetailDto(
    RouteUnlockRequestDto Request,
    IReadOnlyList<RouteUnlockRequestEventDto> Events,
    IReadOnlyList<RouteUnlockBillDto> Bills
);

public record RouteUnlockPendingCountDto(int Count);

public record RouteUnlockPagedResult(
    IReadOnlyList<RouteUnlockRequestDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
