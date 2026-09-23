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
/// Moves a closed (inactive) distributor's remaining stock into an active distributor.
/// Every line is a TransferOut on the source and a TransferIn on the target, written through the
/// shared <see cref="IStockRepository"/> Deduct/Credit primitives in one transaction, so the stock
/// is never counted twice and the ledger reconciles by construction.
/// </summary>
public class StockTransferService(
    IStockTransferRepository repo,
    IStockRepository stockRepo,
    IDistributedLockService lockService,
    AppDbContext db) : IStockTransferService
{
    public const string ReferenceType = "StockTransfer";

    private const int NotesMaxLength = 500;

    private readonly IStockTransferRepository _repo        = repo;
    private readonly IStockRepository         _stockRepo   = stockRepo;
    private readonly IDistributedLockService  _lockService = lockService;
    private readonly AppDbContext             _db          = db;

    public async Task<StockTransferDto> CreateAsync(
        CreateStockTransferRequest request, int callerId, CancellationToken ct = default)
    {
        // The validator already rejects this (400); guarded again so the service is safe on its own.
        if (request.SourceDistributorId == request.TargetDistributorId)
            throw new BusinessRuleException("SAME_DISTRIBUTOR",
                "Target distributor must be different from the source distributor.");

        // One transfer per source at a time — a second concurrent move of the same stock would
        // otherwise race on the source balance.
        await using var advisoryLock = await _lockService.AcquireAsync(
            $"stock-transfer:{request.SourceDistributorId}", ct)
            ?? throw new StockTransferInProgressException(request.SourceDistributorId);

        var source = await _repo.GetDistributorAsync(request.SourceDistributorId, ct)
            ?? throw new NotFoundException("Distributor", request.SourceDistributorId);
        if (source.IsActive)
            throw new BusinessRuleException("SOURCE_DISTRIBUTOR_ACTIVE",
                $"Distributor '{source.Name}' is still active. Only a closed (inactive) distributor's stock can be transferred.",
                new { distributorId = source.Id });

        var target = await _repo.GetDistributorAsync(request.TargetDistributorId, ct)
            ?? throw new NotFoundException("Distributor", request.TargetDistributorId);
        if (!target.IsActive)
            throw new BusinessRuleException("TARGET_DISTRIBUTOR_INACTIVE",
                $"Distributor '{target.Name}' is inactive. Stock can only be transferred to an active distributor.",
                new { distributorId = target.Id });

        var products = await _repo.GetProductsAsync(request.Lines.Select(l => l.ProductId), ct);
        var missingProduct = request.Lines.FirstOrDefault(l => !products.ContainsKey(l.ProductId));
        if (missingProduct is not null)
            throw new NotFoundException("Product", missingProduct.ProductId);

        StockTransfer? transfer = null;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs this body on the same DbContext — drop whatever the failed attempt
            // left tracked so it isn't inserted (or updated) a second time.
            if (transfer is not null) DiscardFailedAttempt();

            await using var tx = await _repo.BeginTransactionAsync(ct);
            try
            {
                // Lock source AND target rows in one SELECT … ORDER BY "Id" FOR UPDATE, so this
                // cannot deadlock against a bill/GRN/transfer touching overlapping rows.
                var keys = request.Lines.SelectMany(l => new[]
                {
                    new StockKey(source.Id, l.ProductId, l.StockType),
                    new StockKey(target.Id, l.ProductId, l.StockType),
                });
                var locked = await _stockRepo.LockStocksForUpdateAsync(keys, ct);

                // Check every line against the locked balance up front, so the caller gets one
                // complete list of shortages rather than failing on the first.
                var shortages = request.Lines
                    .Select(l => new
                    {
                        Line      = l,
                        Available = locked.TryGetValue(new StockKey(source.Id, l.ProductId, l.StockType), out var s)
                                        ? s.QuantityOnHand : 0m,
                    })
                    .Where(x => x.Line.Quantity > x.Available)
                    .Select(x => new StockShortage(
                        x.Line.ProductId,
                        $"{products[x.Line.ProductId].ItemDescription} ({x.Line.StockType})",
                        x.Line.Quantity,
                        x.Available))
                    .ToList();
                if (shortages.Count > 0)
                    throw new InsufficientStockException(shortages);

                var now = DateTime.UtcNow;
                transfer = new StockTransfer
                {
                    // Placeholder — the real number is derived from the Id once it is assigned.
                    TransferNumber      = $"TMP-{Guid.NewGuid():N}"[..30],
                    SourceDistributorId = source.Id,
                    TargetDistributorId = target.Id,
                    Notes               = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    TransferredBy       = callerId,
                    TransferredAt       = now,
                    CreatedAt           = now,
                    UpdatedAt           = now,
                    CreatedBy           = callerId,
                    UpdatedBy           = callerId,
                    Lines = request.Lines.Select(l => new StockTransferLine
                    {
                        ProductId = l.ProductId,
                        StockType = l.StockType,
                        Quantity  = l.Quantity,
                    }).ToList(),
                };
                await _repo.AddAsync(transfer, ct);
                await _repo.SaveChangesAsync(ct);   // assigns transfer.Id for the ledger ReferenceId

                transfer.TransferNumber = $"ST-{transfer.Id:D6}";

                var outNotes = Truncate($"Transferred to {target.Name} ({transfer.TransferNumber})");
                var inNotes  = Truncate($"Transferred from {source.Name} ({transfer.TransferNumber})");

                foreach (var line in request.Lines)
                {
                    await _stockRepo.DeductStockAsync(
                        source.Id, line.ProductId, line.Quantity, line.StockType,
                        StockTransactionType.TransferOut,
                        referenceType: ReferenceType, referenceId: transfer.Id,
                        transactedBy: callerId, notes: outNotes, ct: ct);

                    await _stockRepo.CreditStockAsync(
                        target.Id, line.ProductId, line.Quantity, line.StockType,
                        StockTransactionType.TransferIn,
                        referenceType: ReferenceType, referenceId: transfer.Id,
                        transactedBy: callerId, notes: inNotes, ct: ct);
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

        return await _repo.GetByIdAsync(transfer!.Id, ct)
            ?? throw new DatabaseUnavailableException();
    }

    public async Task<(List<StockTransferSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? distributorId, CancellationToken ct = default)
    {
        var (_, size, skip) = PaginationHelper.Normalize(page, pageSize);
        return await _repo.GetPagedAsync(skip, size, distributorId, ct);
    }

    public async Task<StockTransferDto> GetByIdAsync(int id, CancellationToken ct = default)
        => await _repo.GetByIdAsync(id, ct)
           ?? throw new NotFoundException("StockTransfer", id);

    private void DiscardFailedAttempt()
    {
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is StockTransfer or StockTransferLine
                || (entry.State == EntityState.Added && entry.Entity is StockTransaction or DistributorStock))
                entry.State = EntityState.Detached;
        }
    }

    private static string Truncate(string value)
        => value.Length <= NotesMaxLength ? value : value[..NotesMaxLength];
}
