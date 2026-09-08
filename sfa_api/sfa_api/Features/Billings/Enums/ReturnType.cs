namespace sfa_api.Features.Billings.Enums;

public enum ReturnType
{
    MarketResell = 0,  // Stock credited back IN to distributor
    Damage       = 1,  // Write-off — no stock movement
    Expire       = 2,  // Write-off — no stock movement

    /// <summary>
    /// Goods the distributor declined while reviewing a pending bill — the quantity carved off a
    /// Sale/FreeIssue line by <c>BillingService.AdjustItemsAsync</c>. The stock is credited back at
    /// adjustment time (not at creation like MarketResell), and the line is informational only:
    /// it is NOT added to <c>Billing.ReturnValue</c>, because the parent line was already reduced.
    /// Tracked separately in <c>Billing.DistributorReturnValue</c>.
    /// </summary>
    DistributorReturn = 3
}
