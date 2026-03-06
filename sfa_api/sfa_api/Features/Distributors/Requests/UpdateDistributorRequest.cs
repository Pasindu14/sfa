namespace sfa_api.Features.Distributors.Requests;

public class UpdateDistributorRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal TradeDiscount { get; set; }
    public decimal Commission { get; set; }
    public string? Remark { get; set; }
    public string? VatRegNo { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
