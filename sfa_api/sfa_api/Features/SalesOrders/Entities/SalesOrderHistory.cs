using sfa_api.Features.SalesOrders.Enums;

namespace sfa_api.Features.SalesOrders.Entities;

public class SalesOrderHistory
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public string Action { get; set; } = string.Empty;      // e.g. "ItemsEdited", "RepApproved"
    public SalesOrderStatus? FromStatus { get; set; }
    public SalesOrderStatus? ToStatus { get; set; }
    public int PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
    public string? ItemsSnapshot { get; set; }              // JSON snapshot
}
