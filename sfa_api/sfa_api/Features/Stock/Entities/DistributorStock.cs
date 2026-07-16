using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Fleets.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Entities;

public class DistributorStock
{
    public int Id { get; set; }

    public int DistributorId { get; set; }
    public int ProductId { get; set; }
    public StockType StockType { get; set; } = StockType.Normal;

    /// <summary>
    /// Denormalized from <see cref="Distributor.FleetId"/> so stock can be filtered and grouped by
    /// fleet without a join. Current state — kept in step with the distributor's fleet by the
    /// cascade in DistributorService.UpdateAsync. Deliberately NOT part of the
    /// {DistributorId, ProductId, StockType} unique index: a distributor has exactly one fleet, so
    /// the fleet adds no uniqueness, and a nullable column in a unique index would let Postgres
    /// (which treats NULLs as distinct) create duplicate stock rows for fleet-less distributors.
    /// </summary>
    public int? FleetId { get; set; }

    public decimal QuantityOnHand { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public Distributor Distributor { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Fleet? Fleet { get; set; }
}
