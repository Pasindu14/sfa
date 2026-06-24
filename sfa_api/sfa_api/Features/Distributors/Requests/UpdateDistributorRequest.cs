namespace sfa_api.Features.Distributors.Requests;

public class UpdateDistributorRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Alias { get; set; }
    public decimal TradeDiscount { get; set; }
    public decimal Commission { get; set; }
    public string Category { get; set; } = "A";
    public string? Remark { get; set; }
    public string? VatRegNo { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? TerritoryId { get; set; }
    public int? FleetId { get; set; }

    // Optimistic concurrency token — required (validator rejects 0).
    public uint RowVersion { get; set; }
}
