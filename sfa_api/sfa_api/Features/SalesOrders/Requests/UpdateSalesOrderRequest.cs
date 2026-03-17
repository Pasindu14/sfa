namespace sfa_api.Features.SalesOrders.Requests;

public class UpdateSalesOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}

public class UpdateSalesOrderRequest
{
    public string? Notes { get; set; }
    public List<UpdateSalesOrderItemRequest> Items { get; set; } = [];
}
