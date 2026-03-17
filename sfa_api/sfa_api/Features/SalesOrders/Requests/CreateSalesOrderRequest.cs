namespace sfa_api.Features.SalesOrders.Requests;

public class CreateSalesOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}

public class CreateSalesOrderRequest
{
    // Required only when caller is Admin (resolved server-side for Distributor role)
    public int? DistributorId { get; set; }
    public string? Notes { get; set; }
    public List<CreateSalesOrderItemRequest> Items { get; set; } = [];
}
