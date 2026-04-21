using sfa_api.Features.Fleets.Entities;
using sfa_api.Features.ProductCategories.Entities;

namespace sfa_api.Features.Products.Entities;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;             // Unique item code
    public string ItemDescription { get; set; } = string.Empty;  // Full display name
    public string? PrintDescription { get; set; }                 // Uppercase label for print/reports
    public int PiecesPerPack { get; set; } = 0;                  // Units per pack
    public string? ImageUrl { get; set; }                         // Storage path or URL
    public string? Remarks { get; set; }
    public int? FleetId { get; set; }
    public int? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public Fleet? Fleet { get; set; }
    public ProductCategory? Category { get; set; }
}
