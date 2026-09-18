using sfa_api.Features.Billings.Enums;
using sfa_api.Features.PricingStructures.Entities;
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

    /// <summary>
    /// The pricing structure that priced this line. Null on lines created before pricing
    /// structures existed. Historical: later edits to the structure never touch this line —
    /// <see cref="UnitPrice"/> and <see cref="ListUnitPrice"/> are the frozen prices.
    /// </summary>
    public int? PricingStructureId { get; set; }

    /// <summary>Which structure price the line used (pack, case, or a rep-typed return price). Null on legacy lines.</summary>
    public PriceBasis? PriceBasis { get; set; }

    /// <summary>
    /// The structure's price for <see cref="PriceBasis"/> at billing time — the exact case price on a
    /// Case line (whose <see cref="UnitPrice"/> is the per-pack equivalent). Null on Manual and legacy lines.
    /// </summary>
    public decimal? ListUnitPrice { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Billing Billing { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public BillingItem? SourceBillingItem { get; set; }
    public PricingStructure? PricingStructure { get; set; }
}
