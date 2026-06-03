using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.StockTaking.Entities;

public class StockTakingLine
{
    public int Id { get; set; }

    public int StockTakingSubmissionId { get; set; }
    public int ProductId               { get; set; }

    public StockType StockType { get; set; } = StockType.Normal;

    public decimal CountedQuantity { get; set; }

    // Snapshot of DistributorStock.QuantityOnHand at submission time (0 if no stock row exists)
    public decimal SystemQuantity { get; set; }

    // Stored at submit: CountedQuantity - SystemQuantity
    public decimal Variance { get; set; }

    public bool      IsAdjusted       { get; set; } = false;
    public decimal?  AdjustedQuantity { get; set; }
    public int?      AdjustedBy       { get; set; }
    public DateTime? AdjustedAt       { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public StockTakingSubmission Submission  { get; set; } = null!;
    public Product               Product     { get; set; } = null!;
    public User?                 AdjustedByUser { get; set; }
}
