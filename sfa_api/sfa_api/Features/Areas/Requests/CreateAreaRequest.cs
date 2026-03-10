namespace sfa_api.Features.Areas.Requests;

public class CreateAreaRequest
{
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
}
