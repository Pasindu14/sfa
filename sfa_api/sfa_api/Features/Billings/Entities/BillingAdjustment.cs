using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Billings.Entities;

/// <summary>
/// One round of distributor quantity adjustments on a pending bill. Mirrors the PurchaseOrderHistory
/// convention, but keeps per-line old/new figures in <see cref="BillingAdjustmentLine"/> rather than a
/// JSON snapshot, because the portal and the staff Rep Bills screen render the change per product.
/// </summary>
public class BillingAdjustment
{
    public int Id { get; set; }
    public int BillingId { get; set; }
    public int AdjustedByUserId { get; set; }
    public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
    public decimal OldTotalAmount { get; set; }
    public decimal NewTotalAmount { get; set; }

    // Navigation
    public Billing Billing { get; set; } = null!;
    public User AdjustedBy { get; set; } = null!;
    public ICollection<BillingAdjustmentLine> Lines { get; set; } = [];
}
