using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.DTOs;

public record BillingListDto(
    int Id,
    string BillingNumber,
    DateOnly BillingDate,
    int OutletId,
    string OutletName,
    int SalesRepId,
    string SalesRepName,
    string? SupervisorName,
    int DistributorId,
    string DistributorName,
    decimal TotalAmount,
    RepBillingStatus RepStatus,
    DistributorBillingStatus DistributorStatus,
    PaymentType PaymentType,
    bool IsCashCollected,
    DateTime CreatedAt,
    bool IsAdjusted,
    // Breakdown shown on every bill list: Sales (gross) − Discount − Returns = Total; FreeIssueValue is informational.
    decimal GrossAmount,
    decimal TotalDiscount,
    decimal ReturnValue,
    decimal FreeIssueValue
);
