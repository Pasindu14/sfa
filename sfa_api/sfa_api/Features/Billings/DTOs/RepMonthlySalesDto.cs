namespace sfa_api.Features.Billings.DTOs;

public record RepMonthlySalesDto(int Year, int Month, decimal TotalSales);

public record RepMonthlySalesItemDto(
    int ProductId,
    string ItemCode,
    string ItemName,
    decimal TargetQuantity,        // cases
    decimal SoldQuantity,          // cases (derived: packs / PacksPerCase)
    decimal SoldQuantityPacks,     // raw packs from BillingItems
    decimal SoldAmount,
    decimal AchievementPercent);

public record RepMonthlySalesItemwiseDto(
    int Year,
    int Month,
    decimal TotalTargetQuantity,       // cases
    decimal TotalSoldQuantity,         // cases
    decimal TotalSoldQuantityPacks,    // packs
    decimal TotalSoldAmount,
    IReadOnlyList<RepMonthlySalesItemDto> Items);
