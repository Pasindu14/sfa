namespace sfa_api.Features.Routes.Requests;

public class CreateRouteRequest
{
    public string Name { get; set; } = string.Empty;
    public string PinColor { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DivisionId { get; set; }
}
