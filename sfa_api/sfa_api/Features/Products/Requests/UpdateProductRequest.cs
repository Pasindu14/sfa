namespace sfa_api.Features.Products.Requests;

public class UpdateProductRequest
{
    public string Code { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string? PrintDescription { get; set; }
    public int PiecesPerPack { get; set; }
    public string? ImageUrl { get; set; }
    public string? Remarks { get; set; }
}
