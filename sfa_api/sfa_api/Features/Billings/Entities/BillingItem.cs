using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.Billings.Entities;

public class BillingItem
{
    public int Id { get; set; }
    public int BillingId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }    // Item-level discount % (0–100), default 0
    public decimal DiscountAmount { get; set; }  // Quantity × UnitPrice × DiscountRate / 100, stored
    public decimal TotalPrice { get; set; }      // Quantity × UnitPrice − DiscountAmount, stored
    public bool IsFreeIssue { get; set; } = false;
    public int LineNumber { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Billing Billing { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
