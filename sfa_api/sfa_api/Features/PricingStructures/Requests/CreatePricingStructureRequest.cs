namespace sfa_api.Features.PricingStructures.Requests;

public class CreatePricingStructureRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; } = false;
}
