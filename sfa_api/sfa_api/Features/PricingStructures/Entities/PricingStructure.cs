namespace sfa_api.Features.PricingStructures.Entities;

/// <summary>
/// A named price list. Reps pick one on the phone when billing; every bill line records the
/// structure that priced it. Exactly one non-deleted structure is the default (enforced by a
/// partial unique index), and the default must stay active.
/// </summary>
public class PricingStructure
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Optimistic concurrency — maps to PostgreSQL xmin system column
    public uint RowVersion { get; set; }

    public ICollection<PricingStructureItem> Items { get; set; } = new List<PricingStructureItem>();
}
