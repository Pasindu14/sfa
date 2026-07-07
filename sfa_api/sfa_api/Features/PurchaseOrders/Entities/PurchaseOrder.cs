using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.PurchaseOrders.Enums;

namespace sfa_api.Features.PurchaseOrders.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }

    // Optimistic concurrency token — maps to PostgreSQL xmin (finding #7).
    public uint RowVersion { get; set; }
    public string OrderNumber { get; set; } = string.Empty;   // PO-2026-00001 (auto-generated)
    public int DistributorId { get; set; }
    public Distributor Distributor { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public string? Notes { get; set; }

    // Transition audit trail
    public int? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? RepApprovedBy { get; set; }
    public DateTime? RepApprovedAt { get; set; }
    public int? ManagerApprovedBy { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public int? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public int? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
    public ICollection<PurchaseOrderHistory> History { get; set; } = [];

    // Standard audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}
