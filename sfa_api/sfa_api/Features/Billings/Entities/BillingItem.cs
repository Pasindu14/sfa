using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.Billings.Entities;

public class BillingItem
{
    public int Id { get; set; }
    public int BillingId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public BillingItemType BillingItemType { get; set; } = BillingItemType.Sale;
    public ReturnType? ReturnType { get; set; }
    public FreeIssueSource? FreeIssueSource { get; set; }
    public DateOnly? ExpireDate { get; set; }
    public int LineNumber { get; set; }

    /// <summary>Who this line came from — the rep's original bill, or a distributor review reduction.</summary>
    public BillingItemSource Source { get; set; } = BillingItemSource.SalesRep;

    /// <summary>
    /// On a DistributorReturn line, the Sale/FreeIssue line the returned quantity was carved out of.
    /// Null on rep-entered lines.
    /// </summary>
    public int? SourceBillingItemId { get; set; }

    /// <summary>
    /// The quantity the rep originally billed, captured the first time the distributor reduces this
    /// line. Null while the line has never been adjusted. Repeat adjustments do not overwrite it.
    /// </summary>
    public decimal? OriginalQuantity { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Billing Billing { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public BillingItem? SourceBillingItem { get; set; }
}
