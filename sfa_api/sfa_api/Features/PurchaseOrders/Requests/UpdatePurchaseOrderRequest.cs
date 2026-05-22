namespace sfa_api.Features.PurchaseOrders.Requests;

public class UpdatePurchaseOrderItemRequest
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}

public class UpdatePurchaseOrderRequest
{
    public string? Notes { get; set; }
    public List<UpdatePurchaseOrderItemRequest> Items { get; set; } = [];
}
