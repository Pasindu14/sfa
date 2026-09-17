using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.DTOs;

/// <summary>
/// GET /api/v1/billings/portal/dashboard-summary — the distributor dashboard's billing figures,
/// aggregated server-side over an inclusive Sri Lanka business-date range.
/// Revenue fields exclude rep-cancelled and distributor-rejected bills; count fields count bills as issued.
/// TotalRevenue = approved + pending, non-cancelled bills; TotalCount = all bills.
/// Days holds one zero-filled entry per date in the range, oldest first.
/// </summary>
public record DistributorBillingDashboardDto(
    DateOnly DateFrom,
    DateOnly DateTo,
    decimal TotalRevenue,
    int TotalCount,
    decimal ApprovedRevenue,
    int ApprovedCount,
    decimal PendingRevenue,
    int PendingCount,
    List<DistributorBillingDashboardDayDto> Days);

public record DistributorBillingDashboardDayDto(
    DateOnly Date,
    decimal TotalRevenue,
    int TotalCount,
    decimal ApprovedRevenue,
    int ApprovedCount,
    decimal PendingRevenue,
    int PendingCount);

/// <summary>Repository row: bills grouped by (date, distributor status, rep status).</summary>
public record DistributorBillingDashboardGroupRow(
    DateOnly BillingDate,
    DistributorBillingStatus DistributorStatus,
    RepBillingStatus RepStatus,
    int Count,
    decimal TotalAmount);
