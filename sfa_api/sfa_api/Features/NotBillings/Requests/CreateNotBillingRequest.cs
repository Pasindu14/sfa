using sfa_api.Features.NotBillings.Enums;

namespace sfa_api.Features.NotBillings.Requests;

public class CreateNotBillingRequest
{
    public int OutletId { get; set; }
    public NotBillingReason Reason { get; set; }
    public string? Notes { get; set; }
    public DateOnly? NotBillingDate { get; set; }
}
