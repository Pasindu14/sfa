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
    public bool IsFreeIssue { get; set; } = false;
    public BillingItemType BillingItemType { get; set; } = BillingItemType.Sale;
    public ReturnType? ReturnType { get; set; }
    public DateOnly? ExpireDate { get; set; }
    public int LineNumber { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Billing Billing { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
