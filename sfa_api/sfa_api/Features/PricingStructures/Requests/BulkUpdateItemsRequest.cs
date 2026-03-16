namespace sfa_api.Features.PricingStructures.Requests;

public class BulkUpdateItemsRequest
{
    public List<PricingStructureItemRequest> Items { get; set; } = [];
}

public class PricingStructureItemRequest
{
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? PackPrice { get; set; }
}
