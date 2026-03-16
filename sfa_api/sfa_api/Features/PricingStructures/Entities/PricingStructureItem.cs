using System.ComponentModel.DataAnnotations.Schema;
using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.PricingStructures.Entities;

public class PricingStructureItem
{
    public int Id { get; set; }
    public int PricingStructureId { get; set; }
    public int ProductId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DealerPackPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DealerCasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PromotionalPrice { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public PricingStructure PricingStructure { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
