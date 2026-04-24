using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.DTOs;

public record BillingDto(
    int Id,
    string BillingNumber,
    DateOnly BillingDate,

    // Core references
    int OutletId,
    string OutletName,
    int SalesRepId,
    string SalesRepName,
    int DistributorId,
    string DistributorName,

    // Org chain
    int? SupervisorUserId,
    string? SupervisorName,
    int? AsmUserId,
    string? AsmName,
    int? RsmUserId,
    string? RsmName,
    int? NsmUserId,
    string? NsmName,

    // Geo chain
    int? RouteId,
    int? DivisionId,
    int? TerritoryId,
    int? AreaId,
    int? RegionId,

    // Amounts
    decimal SubTotalAmount,
    decimal BillDiscountRate,
    decimal BillDiscountAmount,
    decimal TotalAmount,

    BillingStatus Status,
    string? Notes,
    double? Latitude,
    double? Longitude,
    DateTime CreatedAt,
    List<BillingItemDto> Items
);
