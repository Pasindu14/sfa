using System.ComponentModel.DataAnnotations.Schema;
using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.ProductCategoryPricings.Entities;

public class ProductCategoryPrice
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Category { get; set; } = string.Empty;  // "A" | "B" | "C" | "D"

    [Column(TypeName = "decimal(18,4)")]
    public decimal Price { get; set; } = 0m;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public Product? Product { get; set; }
}
