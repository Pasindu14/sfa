namespace sfa_api.Features.Billings.Entities;

/// <summary>One adjusted bill line within a <see cref="BillingAdjustment"/> round.</summary>
public class BillingAdjustmentLine
{
    public int Id { get; set; }
    public int BillingAdjustmentId { get; set; }

    /// <summary>The Sale/FreeIssue line that was reduced.</summary>
    public int BillingItemId { get; set; }
    public int ProductId { get; set; }

    public decimal OldQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public decimal OldTotalPrice { get; set; }
    public decimal NewTotalPrice { get; set; }

    /// <summary>OldQuantity − NewQuantity — the quantity written to the DistributorReturn line.</summary>
    public decimal ReturnedQuantity { get; set; }

    /// <summary>Value of the returned quantity, discounted on the same basis as the original line.</summary>
    public decimal ReturnValue { get; set; }

    // Navigation
    public BillingAdjustment BillingAdjustment { get; set; } = null!;
}
