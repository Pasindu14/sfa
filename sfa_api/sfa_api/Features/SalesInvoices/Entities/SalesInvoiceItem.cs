using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.SalesInvoices.Entities;

public class SalesInvoiceItem
{
    public int Id { get; set; }
    public int SalesInvoiceId { get; set; }
    public int ProductId { get; set; }
    public string ItemErpCode { get; set; } = string.Empty;     // raw BUSY alias e.g. CF01
    public string ItemDescription { get; set; } = string.Empty; // raw description from Excel
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;            // CTN / PCS
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsFreeIssue { get; set; }
    public int LineNumber { get; set; }                         // 1-based order within invoice

    // Navigation
    public SalesInvoice SalesInvoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
