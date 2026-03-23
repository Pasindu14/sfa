namespace sfa_api.Features.ProductCategoryPricings.Requests;

public class PricingRowRequest
{
    public int ProductId { get; set; }
    public decimal PriceA { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
    public decimal PriceD { get; set; }
}

public class BulkUpsertPricingRequest
{
    public IEnumerable<PricingRowRequest> Items { get; set; } = [];
}
