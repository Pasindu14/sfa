using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.DTOs;

/// <summary>
/// A rep's own bill, shaped for re-hydrating the mobile app's local outbox after a reinstall or a
/// device change. Deliberately slimmer than <see cref="BillingDto"/> — the phone has no use for the
/// org/geo chains — but it carries the two things the list DTO lacks and the sync needs:
/// <c>ClientBillId</c>, which is how a downloaded bill is matched against a row the device already
/// holds, and the line items.
/// </summary>
public record RepBillSyncDto(
    int Id,
    string BillingNumber,
    string? ClientBillId,
    DateOnly BillingDate,
    int OutletId,
    string OutletName,
    decimal SubTotalAmount,
    decimal BillDiscountRate,
    decimal BillDiscountAmount,
    decimal TotalAmount,
    string? Notes,
    double? Latitude,
    double? Longitude,
    RepBillingStatus RepStatus,
    DistributorBillingStatus DistributorStatus,
    DateTime CreatedAt,
    List<RepBillSyncItemDto> Items
);

public record RepBillSyncItemDto(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    BillingItemType BillingItemType,
    ReturnType? ReturnType,
    FreeIssueSource? FreeIssueSource,
    DateOnly? ExpireDate,
    int LineNumber
);
