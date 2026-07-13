namespace sfa_api.Features.GeoConsistency.Entities;

/// <summary>
/// One geo-consistency reconciliation pass (nightly or on-demand). Proves that every live descendant's
/// denormalized ancestor IDs still match the ancestor derived from its parent chain — i.e. that no
/// re-parent cascade was ever missed. Append-only; purged on a retention window. A row is written even
/// for a clean pass so a dashboard can distinguish "ran &amp; clean" from "never ran".
/// </summary>
public class GeoConsistencyRun
{
    public int Id { get; set; }

    public DateTime RunAt { get; set; } = DateTime.UtcNow;

    /// <summary>"nightly" for the scheduled job, or "manual:{userId}" for an on-demand admin run.</summary>
    public string TriggeredBy { get; set; } = "nightly";

    /// <summary>Total live (non-deleted) rows examined across all five descendant tables.</summary>
    public int RowsChecked { get; set; }

    /// <summary>Number of drifted rows found (0 = every denormalized ancestor matches its parent).</summary>
    public int DriftCount { get; set; }

    public int DurationMs { get; set; }

    public ICollection<GeoConsistencyFlag> Flags { get; set; } = new List<GeoConsistencyFlag>();
}
