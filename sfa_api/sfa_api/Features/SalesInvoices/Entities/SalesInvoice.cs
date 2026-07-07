using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.PurchaseOrders.Entities;
using sfa_api.Features.SalesInvoices.Enums;

namespace sfa_api.Features.SalesInvoices.Entities;

public class SalesInvoice
{
    public int Id { get; set; }

    // Optimistic concurrency token — maps to PostgreSQL xmin (finding #7).
    public uint RowVersion { get; set; }
    public string VchBillNo { get; set; } = string.Empty;          // BIS/25/4752 — idempotency key
    public string? BusyOrderRequestNo { get; set; }                 // SOR-26-00010 — raw BUSY ref
    public string? SfaPoNumber { get; set; }                        // PO-2026-00001 — raw string for audit
    public int? PurchaseOrderId { get; set; }                       // resolved FK; null if PO not found
    public int DistributorId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public SalesInvoiceType InvoiceType { get; set; } = SalesInvoiceType.Regular;
    public decimal TotalAmount { get; set; }
    public int ImportBatchId { get; set; }
    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Pending;
    public string? Notes { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Distributor Distributor { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public SalesInvoiceImportBatch ImportBatch { get; set; } = null!;
    public ICollection<SalesInvoiceItem> Items { get; set; } = [];
}
