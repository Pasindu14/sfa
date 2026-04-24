using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Outlets.Entities;

public enum OutletType { Small, Medium, Large }
public enum OutletCategory { Wholesale, SMMT }

public class Outlet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Tel { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string NicNo { get; set; } = string.Empty;
    public string? VatNo { get; set; }
    public decimal CreditLimit { get; set; } = 0;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime? OwnerDOB { get; set; }
    public string? Remarks { get; set; }
    public string? Image { get; set; }
    public OutletType OutletType { get; set; }
    public OutletCategory OutletCategory { get; set; }
    public int? ProvinceCode { get; set; }
    public int? DistrictCode { get; set; }

    // Ancestor IDs (denormalized)
    public int RouteId { get; set; }      // direct parent (FK)
    public int DivisionId { get; set; }   // denormalized ancestor
    public int TerritoryId { get; set; }  // denormalized ancestor
    public int AreaId { get; set; }       // denormalized ancestor
    public int RegionId { get; set; }     // denormalized ancestor

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public RouteEntity? Route { get; set; }
}
