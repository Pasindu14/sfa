using sfa_api.Features.PurchaseOrders.Enums;

namespace sfa_api.Features.PurchaseOrders.Entities;

public class PurchaseOrderHistory
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public string Action { get; set; } = string.Empty;      // e.g. "ItemsEdited", "RepApproved"
    public PurchaseOrderStatus? FromStatus { get; set; }
    public PurchaseOrderStatus? ToStatus { get; set; }
    public int PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
    public string? ItemsSnapshot { get; set; }              // JSON snapshot
}
