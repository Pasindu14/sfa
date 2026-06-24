using System.Diagnostics;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Repositories;

namespace sfa_api.Features.Stock.Services;

/// <summary>
/// Proves the stock ledger is self-consistent: opening + GRN received − billed − adjustments + returns
/// (== Σ In − Σ Out) must equal the live DistributorStock.QuantityOnHand for every group. Read-only
/// over the live data; the only writes are the run/flag audit rows (review finding #4).
/// </summary>
public class StockReconciliationService(
    IStockReconciliationRepository repo,
    ILogger<StockReconciliationService> logger) : IStockReconciliationService
{
    private readonly IStockReconciliationRepository _repo = repo;
    private readonly ILogger<StockReconciliationService> _logger = logger;

    public async Task<StockReconciliationResultDto> RunAsync(
        int? distributorId, int? productId, string triggeredBy, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var ledgerNets = await _repo.GetLedgerNetsAsync(distributorId, productId, ct);
        var snapshots  = await _repo.GetLatestSnapshotsAsync(distributorId, productId, ct);
        var onHand     = await _repo.GetOnHandAsync(distributorId, productId, ct);

        var discrepancies = StockReconciliation.Reconcile(ledgerNets, snapshots, onHand);

        // Groups examined = the distinct keys seen across every source (so an orphan on either side counts).
        var groupsChecked = ledgerNets.Select(n => n.Key)
            .Concat(snapshots.Select(s => s.Key))
            .Concat(onHand.Keys)
            .ToHashSet()
            .Count;

        var run = new StockReconciliationRun
        {
            RunAt            = DateTime.UtcNow,
            TriggeredBy      = triggeredBy,
            GroupsChecked    = groupsChecked,
            DiscrepancyCount = discrepancies.Count,
            Flags = discrepancies.Select(d => new StockReconciliationFlag
            {
                DistributorId    = d.DistributorId,
                ProductId        = d.ProductId,
                StockType        = d.StockType,
                Kind             = d.Kind,
                ExpectedQuantity = d.ExpectedQuantity,
                ActualQuantity   = d.ActualQuantity,
                Delta            = d.Delta,
            }).ToList()
        };
        run.DurationMs = (int)sw.ElapsedMilliseconds;

        await _repo.SaveRunAsync(run, ct);

        if (discrepancies.Count == 0)
            _logger.LogInformation(
                "Stock reconciliation ({TriggeredBy}) clean: {Groups} groups, 0 discrepancies in {Ms}ms",
                triggeredBy, groupsChecked, run.DurationMs);
        else
        {
            _logger.LogWarning(
                "Stock reconciliation ({TriggeredBy}) found {Count} discrepancies across {Groups} groups",
                triggeredBy, discrepancies.Count, groupsChecked);
            foreach (var d in discrepancies)
                _logger.LogWarning(
                    "Stock drift [{Kind}] distributor {DistributorId} product {ProductId} {StockType}: expected={Expected} actual={Actual} delta={Delta}",
                    d.Kind, d.DistributorId, d.ProductId, d.StockType, d.ExpectedQuantity, d.ActualQuantity, d.Delta);
        }

        var dtos = await EnrichAsync(run.Id, discrepancies, ct);
        return new StockReconciliationResultDto(
            run.Id, run.RunAt, run.TriggeredBy, run.GroupsChecked, run.DiscrepancyCount, dtos);
    }

    public async Task<StockReconciliationResultDto?> GetLatestRunAsync(CancellationToken ct = default)
    {
        var run = await _repo.GetLatestRunAsync(ct);
        if (run is null) return null;

        var discrepancies = run.Flags
            .Select(f => new StockReconciliation.Discrepancy(
                f.DistributorId, f.ProductId, f.StockType, f.Kind,
                f.ExpectedQuantity, f.ActualQuantity, f.Delta))
            .ToList();

        var dtos = await EnrichAsync(run.Id, discrepancies, ct);
        return new StockReconciliationResultDto(
            run.Id, run.RunAt, run.TriggeredBy, run.GroupsChecked, run.DiscrepancyCount, dtos);
    }

    // Joins distributor names + product codes onto the flags for display.
    private async Task<List<StockDiscrepancyDto>> EnrichAsync(
        int runId, IReadOnlyList<StockReconciliation.Discrepancy> discrepancies, CancellationToken ct)
    {
        if (discrepancies.Count == 0) return [];

        var (distributorNames, productCodes) = await _repo.GetNamesAsync(
            discrepancies.Select(d => d.DistributorId),
            discrepancies.Select(d => d.ProductId), ct);

        return discrepancies.Select(d => new StockDiscrepancyDto(
            d.DistributorId,
            distributorNames.GetValueOrDefault(d.DistributorId, $"#{d.DistributorId}"),
            d.ProductId,
            productCodes.GetValueOrDefault(d.ProductId, $"#{d.ProductId}"),
            d.StockType.ToString(),
            d.Kind.ToString(),
            d.ExpectedQuantity,
            d.ActualQuantity,
            d.Delta)).ToList();
    }
}
