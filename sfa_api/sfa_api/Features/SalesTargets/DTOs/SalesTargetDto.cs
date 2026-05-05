namespace sfa_api.Features.SalesTargets.DTOs;

public record RepMonthlyTargetDto(int Year, int Month, decimal TotalTarget);

public record SalesTargetDto(
    int     Id,
    int     ImportBatchId,
    int     Year,
    int     Month,
    int     SalesRepId,
    string  SalesRepName,
    int     ProductId,
    string  ProductCode,
    string  ProductName,
    decimal TargetQuantity,
    int?    SupervisorUserId,
    string? SupervisorName,
    int?    AsmUserId,
    int?    RsmUserId,
    int?    NsmUserId,
    int?    DistributorId,
    int?    DivisionId,
    int?    TerritoryId,
    int?    AreaId,
    int?    RegionId,
    DateTime UpdatedAt
);
