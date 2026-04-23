using sfa_api.Features.NotBillings.Enums;

namespace sfa_api.Features.NotBillings.DTOs;

public record NotBillingListDto(
    int Id,
    string NotBillingNumber,
    DateOnly NotBillingDate,
    int OutletId,
    string OutletName,
    int SalesRepId,
    string SalesRepName,
    NotBillingReason Reason,
    DateTime CreatedAt
);
