namespace sfa_api.Features.PurchaseOrders.Requests;

public class CreatePurchaseOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}

public class CreatePurchaseOrderRequest
{
    // Required only when caller is Admin (resolved server-side for Distributor role)
    public int? DistributorId { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = [];
}
