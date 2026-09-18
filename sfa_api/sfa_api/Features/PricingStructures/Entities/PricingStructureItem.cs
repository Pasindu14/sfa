using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.PricingStructures.Entities;

/// <summary>
/// One product's prices inside a <see cref="PricingStructure"/>. A null <see cref="DealerPackPrice"/>
/// means the product is not priced in this structure (the phone cannot bill it). Rows are never
/// removed — clearing a price sets it to null.
/// </summary>
public class PricingStructureItem
{
    public int Id { get; set; }
    public int PricingStructureId { get; set; }
    public int ProductId { get; set; }
    public decimal? DealerPackPrice { get; set; }
    public decimal? DealerCasePrice { get; set; }
    public decimal? Mrp { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public PricingStructure PricingStructure { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
