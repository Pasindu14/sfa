namespace sfa_api.Features.Outlets.Requests;

public class UpdateOutletRequest
{
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
    public string OutletType { get; set; } = string.Empty;
    public string OutletCategory { get; set; } = string.Empty;
    public string? BillingPriceType { get; set; }
    public int? ProvinceCode { get; set; }
    public int? DistrictCode { get; set; }
    public int RouteId { get; set; }
}
