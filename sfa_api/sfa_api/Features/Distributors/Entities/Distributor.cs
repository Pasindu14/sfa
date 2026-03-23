using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;

namespace sfa_api.Features.Distributors.Entities;

public class Distributor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Alias { get; set; }
    public decimal TradeDiscount { get; set; }
    public decimal Commission { get; set; }
    public string Category { get; set; } = "A";  // "A" | "B" | "C" | "D"
    public string? Remark { get; set; }
    public string? VatRegNo { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Geographic hierarchy (denormalized — nullable because existing distributors may not be assigned yet)
    public int? TerritoryId { get; set; }  // direct parent (FK)
    public int? AreaId { get; set; }       // denormalized ancestor
    public int? RegionId { get; set; }     // denormalized ancestor

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Territory? Territory { get; set; }
    public Area? Area { get; set; }
    public Region? Region { get; set; }
}
