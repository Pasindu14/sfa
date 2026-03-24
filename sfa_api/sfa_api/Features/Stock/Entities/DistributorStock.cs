using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.Stock.Entities;

public class DistributorStock
{
    public int Id { get; set; }

    public int DistributorId { get; set; }
    public int ProductId { get; set; }

    public decimal QuantityOnHand { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public Distributor Distributor { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
