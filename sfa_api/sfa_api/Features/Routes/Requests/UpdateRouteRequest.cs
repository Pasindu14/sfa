namespace sfa_api.Features.Routes.Requests;

public class UpdateRouteRequest
{
    public string Name { get; set; } = string.Empty;
    public string PinColor { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DivisionId { get; set; }
}
