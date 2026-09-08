namespace sfa_api.Features.Billings.Enums;

/// <summary>
/// Distinguishes a line the sales rep entered on the original bill from one the system generated
/// when the distributor reduced a quantity during review.
/// </summary>
public enum BillingItemSource
{
    SalesRep          = 0,
    DistributorReturn = 1
}
