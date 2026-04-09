using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.DTOs;

public record BillingListDto(
    int Id,
    string BillingNumber,
    BillingType BillingType,
    ReturnType? ReturnType,
    DateOnly BillingDate,
    int OutletId,
    string OutletName,
    int SalesRepId,
    string SalesRepName,
    int DistributorId,
    string DistributorName,
    decimal TotalAmount,
    BillingStatus Status,
    DateTime CreatedAt
);
