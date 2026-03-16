namespace sfa_api.Features.PricingStructures.Requests;

public class BulkUpdateItemsRequest
{
    public List<PricingStructureItemRequest> Items { get; set; } = [];
}

public class PricingStructureItemRequest
{
    public int ProductId { get; set; }
    public decimal? DealerPackPrice { get; set; }
    public decimal? DealerCasePrice { get; set; }
    public decimal? PromotionalPrice { get; set; }
}
