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
    BillingStatus Status,
    DateTime CreatedAt
);
