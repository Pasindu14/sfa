namespace sfa_api.Features.Billings.DTOs;

public record RepMonthlySalesDto(int Year, int Month, decimal TotalSales, decimal PendingTotal);

public record RepDailySalesDto(DateOnly Date, decimal ApprovedTotal, decimal PendingTotal);

public record RepMonthlySalesItemDto(
    int ProductId,
    string ItemCode,
    string ItemName,
    decimal TargetQuantity,        // cases
    decimal SoldQuantity,          // cases (derived: packs / PacksPerCase)
    decimal SoldQuantityPacks,     // raw packs from BillingItems
    decimal SoldAmount,
    decimal AchievementPercent,
    decimal ReturnQuantityPacks = 0m,       // outlet returns already subtracted from the sold figures
    decimal FreeIssueQuantityPacks = 0m,    // informational, never counted as sold
    decimal PendingQuantityPacks = 0m,      // pending-approval bills, not yet in the sold figures
    decimal PendingQuantity = 0m,           // same, in cases
    bool HasTarget = false);

public record RepMonthlySalesItemwiseDto(
    int Year,
    int Month,
    decimal TotalTargetQuantity,       // cases
    decimal TotalSoldQuantity,         // cases
    decimal TotalSoldQuantityPacks,    // packs
    decimal TotalSoldAmount,
    IReadOnlyList<RepMonthlySalesItemDto> Items,
    decimal TotalReturnQuantityPacks = 0m,
    decimal TotalFreeIssueQuantityPacks = 0m,
    decimal TotalPendingQuantityPacks = 0m,
    decimal OverallAchievementPercent = 0m);   // sold cases of targeted items / total target cases
