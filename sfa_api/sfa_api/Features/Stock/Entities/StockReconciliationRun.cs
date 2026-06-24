namespace sfa_api.Features.Stock.Entities;

/// <summary>
/// One stock-reconciliation pass (nightly or on-demand). Append-only; purged on a retention window.
/// Persisting a row per run — even a clean one with zero flags — lets a dashboard show
/// "last run 03:00 — 0 discrepancies" and distinguish "ran &amp; clean" from "never ran" (finding #4).
/// </summary>
public class StockReconciliationRun
{
    public int Id { get; set; }

    public DateTime RunAt { get; set; } = DateTime.UtcNow;

    /// <summary>"nightly" for the scheduled job, or "manual:{userId}" for an on-demand admin run.</summary>
    public string TriggeredBy { get; set; } = "nightly";

    /// <summary>How many (distributor, product, stock-type) groups were examined.</summary>
    public int GroupsChecked { get; set; }

    /// <summary>Number of flags this run produced (0 = ledger fully self-consistent).</summary>
    public int DiscrepancyCount { get; set; }

    public int DurationMs { get; set; }

    public ICollection<StockReconciliationFlag> Flags { get; set; } = new List<StockReconciliationFlag>();
}
