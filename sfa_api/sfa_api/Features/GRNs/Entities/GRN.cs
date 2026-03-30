using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.GRNs.Enums;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.GRNs.Entities;

public class GRN
{
    public int Id { get; set; }

    /// <summary>Auto-generated: GRN-2026-00001</summary>
    public string GrnNumber { get; set; } = string.Empty;

    /// <summary>Unique FK — enforces 1:1 with SalesInvoice</summary>
    public int SalesInvoiceId { get; set; }

    /// <summary>Denormalized from SalesInvoice for fast distributor queries</summary>
    public int DistributorId { get; set; }

    public GrnStatus Status { get; set; } = GrnStatus.Pending;

    public DateTime? ReceivedAt { get; set; }
    public int? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // ── Navigation ────────────────────────────────────────────────────────
    public SalesInvoice SalesInvoice { get; set; } = null!;
    public Distributor Distributor { get; set; } = null!;
    public User? ConfirmedByUser { get; set; }
    public ICollection<GRNItem> Items { get; set; } = new List<GRNItem>();
}
