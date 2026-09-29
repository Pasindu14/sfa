using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Supervisor.DTOs;

/// <summary>
/// One rep's billing totals over a date range, for the supervisor Sales Summary page.
/// A bill is a sale only once the distributor approved it and the rep did not cancel it;
/// money figures cover live bills (approved + pending), rejected/cancelled are counts only.
/// </summary>
public record RepBillingSummaryDto(
    DateOnly From,
    DateOnly To,
    int TotalBills,
    int ApprovedCount,
    int PendingCount,
    int RejectedCount,
    int CancelledCount,
    decimal TotalBilled,
    decimal ApprovedSales,
    decimal PendingValue,
    decimal TotalDiscount,
    decimal GoodReturn,
    decimal MarketReturn);

/// <summary>Bills of one (RepStatus, DistributorStatus) pair: count and header sums.</summary>
public record RepBillingStatusGroupRow(
    RepBillingStatus RepStatus,
    DistributorBillingStatus DistributorStatus,
    int Count,
    decimal TotalAmount,
    decimal TotalDiscount);

/// <summary>Return-line sums over live bills. Good = MarketResell, Market = Damage + Expire.</summary>
public record RepBillingReturnTotals(decimal GoodReturn, decimal MarketReturn);
