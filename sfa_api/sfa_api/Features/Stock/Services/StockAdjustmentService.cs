using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Services;

/// <summary>
/// Admin correction of a distributor's stock balance (active or closed). The admin sends the new
/// balance per line; the signed difference against the locked balance is posted as a Correction
/// ledger row (Credit for an increase, Deduct for a decrease) through the shared
/// <see cref="IStockRepository"/> primitives, so the ledger reconciles by construction and the bin
/// card shows it in the Stock Adjustment column.
/// </summary>
public class StockAdjustmentService(
    IStockAdjustmentRepository repo,
    IStockRepository stockRepo,
    IDistributedLockService lockService,
    AppDbContext db) : IStockAdjustmentService
{
    public const string ReferenceType = "StockAdjustment";

    private readonly IStockAdjustmentRepository _repo        = repo;
    private readonly IStockRepository           _stockRepo   = stockRepo;
    private readonly IDistributedLockService    _lockService = lockService;
    private readonly AppDbContext               _db          = db;

    public async Task<StockAdjustmentDto> CreateAsync(
        CreateStockAdjustmentRequest request, int callerId, CancellationToken ct = default)
    {
        // One adjustment per distributor at a time — two admins correcting the same balances would
        // otherwise both pass the expected-quantity check against the same snapshot.
        await using var advisoryLock = await _lockService.AcquireAsync(
            $"stock-adjustment:{request.DistributorId}", ct)
            ?? throw new StockAdjustmentInProgressException(request.DistributorId);

        // Active or closed — both may need their balance corrected.
        var distributor = await _repo.GetDistributorAsync(request.DistributorId, ct)
            ?? throw new NotFoundException("Distributor", request.DistributorId);

        var products = await _repo.GetProductsAsync(request.Lines.Select(l => l.ProductId), ct);
        var missingProduct = request.Lines.FirstOrDefault(l => !products.ContainsKey(l.ProductId));
        if (missingProduct is not null)
            throw new NotFoundException("Product", missingProduct.ProductId);

        StockAdjustment? adjustment = null;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs this body on the same DbContext — drop whatever the failed attempt
            // left tracked so it isn't inserted (or updated) a second time.
            if (adjustment is not null) DiscardFailedAttempt();

            await using var tx = await _repo.BeginTransactionAsync(ct);
            try
            {
                var locked = await _stockRepo.LockStocksForUpdateAsync(
                    request.Lines.Select(l => new StockKey(distributor.Id, l.ProductId, l.StockType)), ct);

                // A missing stock row is a balance of 0 (CreditStockAsync creates it on an increase).
                var lines = request.Lines
                    .Select(l => new
                    {
                        Line    = l,
                        Current = locked.TryGetValue(new StockKey(distributor.Id, l.ProductId, l.StockType), out var s)
                                      ? s.QuantityOnHand : 0m,
                    })
                    .ToList();

                // The admin typed new balances against what they saw; if any balance moved since,
                // the differences would be wrong — make the client reload rather than guess.
                var changed = lines
                    .Where(x => x.Current != x.Line.ExpectedQuantity)
                    .Select(x => new StockBalanceChange(
                        x.Line.ProductId,
                        products[x.Line.ProductId].ItemDescription,
                        x.Line.StockType.ToString(),
                        x.Line.ExpectedQuantity,
                        x.Current))
                    .ToList();
                if (changed.Count > 0)
                    throw new StockChangedException(changed);

                var effective = lines
                    .Select(x => new
                    {
                        x.Line,
                        Before     = x.Current,
                        Difference = x.Line.NewQuantity - x.Current,
                    })
                    .Where(x => x.Difference != 0m)
                    .ToList();
                if (effective.Count == 0)
                    throw new BusinessRuleException("NO_CHANGES",
                        "None of the lines change the stock balance.");

                var now = DateTime.UtcNow;
                adjustment = new StockAdjustment
                {
                    // Placeholder — the real number is derived from the Id once it is assigned.
                    AdjustmentNumber = $"TMP-{Guid.NewGuid():N}"[..30],
                    DistributorId    = distributor.Id,
                    Reason           = request.Reason,
                    Notes            = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    AdjustedBy       = callerId,
                    AdjustedAt       = now,
                    CreatedAt        = now,
                    UpdatedAt        = now,
                    CreatedBy        = callerId,
                    UpdatedBy        = callerId,
                    Lines = effective.Select(x => new StockAdjustmentLine
                    {
                        ProductId      = x.Line.ProductId,
                        StockType      = x.Line.StockType,
                        QuantityBefore = x.Before,
                        NewQuantity    = x.Line.NewQuantity,
                        Difference     = x.Difference,
                    }).ToList(),
                };
                await _repo.AddAsync(adjustment, ct);
                await _repo.SaveChangesAsync(ct);   // assigns adjustment.Id for the ledger ReferenceId

                adjustment.AdjustmentNumber = $"SA-{adjustment.Id:D6}";
                var notes = $"Stock adjustment {adjustment.AdjustmentNumber} ({request.Reason})";

                foreach (var x in effective)
                {
                    if (x.Difference > 0)
                        await _stockRepo.CreditStockAsync(
                            distributor.Id, x.Line.ProductId, x.Difference, x.Line.StockType,
                            StockTransactionType.Correction,
                            referenceType: ReferenceType, referenceId: adjustment.Id,
                            transactedBy: callerId, notes: notes, ct: ct);
                    else
                        await _stockRepo.DeductStockAsync(
                            distributor.Id, x.Line.ProductId, -x.Difference, x.Line.StockType,
                            StockTransactionType.Correction,
                            referenceType: ReferenceType, referenceId: adjustment.Id,
                            transactedBy: callerId, notes: notes, ct: ct);
                }

                await _stockRepo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        return await _repo.GetByIdAsync(adjustment!.Id, ct)
            ?? throw new DatabaseUnavailableException();
    }

    public async Task<(List<StockAdjustmentSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? distributorId, CancellationToken ct = default)
    {
        var (_, size, skip) = PaginationHelper.Normalize(page, pageSize);
        return await _repo.GetPagedAsync(skip, size, distributorId, ct);
    }

    public async Task<StockAdjustmentDto> GetByIdAsync(int id, CancellationToken ct = default)
        => await _repo.GetByIdAsync(id, ct)
           ?? throw new NotFoundException("StockAdjustment", id);

    private void DiscardFailedAttempt()
    {
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is StockAdjustment or StockAdjustmentLine
                || (entry.State == EntityState.Added && entry.Entity is StockTransaction or DistributorStock))
                entry.State = EntityState.Detached;
        }
    }
}
