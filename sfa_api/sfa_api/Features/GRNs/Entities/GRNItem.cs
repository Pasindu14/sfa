using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.GRNs.Entities;

public class GRNItem
{
    public int Id { get; set; }

    public int GrnId { get; set; }
    public int ProductId { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsFreeIssue { get; set; }
    public string? Notes { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public GRN GRN { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
