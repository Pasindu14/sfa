using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.GRNs.DTOs;
using sfa_api.Features.GRNs.Entities;
using sfa_api.Features.GRNs.Enums;
using sfa_api.Features.GRNs.Repositories;
using sfa_api.Features.GRNs.Requests;
using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using Microsoft.EntityFrameworkCore;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.GRNs.Services;

public class GrnService(IGrnRepository repository, IDistributedLockService lockService, AppDbContext db) : IGrnService
{
    private readonly IGrnRepository _repository = repository;
    private readonly IDistributedLockService _lockService = lockService;
    private readonly AppDbContext _db = db;

    // ── List GRNs ─────────────────────────────────────────────────────────

    public async Task<(List<GrnDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? status, int? distributorId, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? search = null, CancellationToken ct = default)
    {
        var (grns, total) = await _repository.GetListAsync(page, pageSize, status, distributorId, dateFrom, dateTo, search, ct);
        var dtos = grns.Select(g => new GrnDto(
            g.Id,
            g.GrnNumber,
            g.SalesInvoiceId,
            g.SalesInvoice?.VchBillNo ?? string.Empty,
            g.DistributorId,
            g.Distributor?.Name ?? string.Empty,
            g.Status.ToString(),
            g.ReceivedAt,
            g.ConfirmedBy,
            null, // ConfirmedByName not loaded in list query
            g.ConfirmedAt,
            g.Notes,
            g.CreatedAt,
            g.SalesInvoice?.TotalAmount ?? 0,
            []
        )).ToList();
        return (dtos, total);
    }

    // ── Create GRN ────────────────────────────────────────────────────────

    public async Task<GrnDto> CreateAsync(CreateGrnRequest request, int callerId, CancellationToken ct = default)
    {
        // 0. Prevent two concurrent GRN creates for the same invoice
        await using var advisoryLock = await _lockService.AcquireAsync($"grn:create:{request.SalesInvoiceId}", ct)
            ?? throw new ConcurrencyConflictException(new { salesInvoiceId = request.SalesInvoiceId, message = "Another GRN creation is already in progress for this invoice." });

        // 1. Load invoice + items
        var invoice = await _repository.GetSalesInvoiceWithItemsAsync(request.SalesInvoiceId, ct)
            ?? throw new NotFoundException("SalesInvoice", request.SalesInvoiceId);

        // 2. Business rule: only Pending invoices can receive a GRN
        if (invoice.Status != SalesInvoiceStatus.Pending)
            throw new BusinessRuleException(
                "GRN_INVOICE_NOT_PENDING",
                $"Cannot create GRN for invoice with status '{invoice.Status}'. Only Pending invoices are eligible.");

        // 3. DB-level guard: unique index prevents two GRNs for the same invoice,
        //    but check here for a clear error message before hitting the constraint
        if (await _repository.GrnExistsForInvoiceAsync(request.SalesInvoiceId, ct))
            throw new DuplicateResourceException("GRN for this invoice");

        // 4. Generate GRN number
        var seqNo = await _repository.GetNextGrnNumberAsync(ct);
        var grnNumber = $"GRN-{SriLankaTime.Year}-{seqNo:D5}";

        // 5. Build GRN + copy items as snapshot
        var grn = new GRN
        {
            GrnNumber       = grnNumber,
            SalesInvoiceId  = invoice.Id,
            DistributorId   = invoice.DistributorId,
            Status          = GrnStatus.Pending,
            CreatedByUserId = callerId,
            UpdatedByUserId = callerId,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow,
        };

        // Guard: duplicate ProductId in invoice items would cause a silent double stock credit
        var duplicateProductIds = invoice.Items
            .GroupBy(i => i.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateProductIds.Count > 0)
            throw new BusinessRuleException(
                "GRN_DUPLICATE_PRODUCT",
                $"Invoice contains duplicate product entries for ProductId(s): {string.Join(", ", duplicateProductIds)}. " +
                "Each product must appear only once per GRN.");

        foreach (var item in invoice.Items)
        {
            grn.Items.Add(new GRNItem
            {
                ProductId   = item.ProductId,
                Quantity    = item.Quantity,
                Unit        = item.Unit,
                IsFreeIssue = item.IsFreeIssue,
            });
        }

        // 6. Mark invoice as GRN received
        invoice.Status    = SalesInvoiceStatus.GrnReceived;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _repository.AddGrnAsync(grn, ct);
        await _repository.SaveChangesAsync(ct);

        // 7. Reload with navigation props for DTO projection
        var created = await _repository.GetGrnWithItemsReadOnlyAsync(grn.Id, ct)
            ?? throw new DatabaseUnavailableException();

        return ProjectToDto(created);
    }

    // ── Confirm GRN ───────────────────────────────────────────────────────

    public async Task<GrnDto> ConfirmAsync(int grnId, ConfirmGrnRequest request, int callerId, int? distributorScopeId = null, CancellationToken ct = default)
    {
        // 1. Acquire advisory lock to prevent concurrent confirms for the same GRN
        await using var advisoryLock = await _lockService.AcquireAsync($"grn:confirm:{grnId}", ct)
            ?? throw new ConcurrencyConflictException(new { grnId, message = "Another confirm operation is already in progress for this GRN." });

        // 2. Load GRN
        var grn = await _repository.GetGrnWithItemsAsync(grnId, ct)
            ?? throw new NotFoundException("GRN", grnId);

        // 2a. Ownership (Distributor caller) — verified inside the lock against the freshly
        //     loaded GRN, not on a stale pre-read, so it cannot race the status transition.
        if (distributorScopeId.HasValue && grn.DistributorId != distributorScopeId.Value)
            throw new BusinessRuleException("GRN_NOT_FOUND", "GRN not found.");

        // 3. Business rule: only Pending GRNs can be confirmed
        if (grn.Status != GrnStatus.Pending)
            throw new BusinessRuleException(
                "GRN_NOT_PENDING",
                $"Cannot confirm GRN with status '{grn.Status}'. Only Pending GRNs can be confirmed.",
                new { grnId, currentStatus = grn.Status.ToString() });

        // 3a. The distributor's fleet — denormalized onto any stock row this receipt creates, and
        //     snapshotted onto every ledger entry it appends. Read off the already-Included
        //     Distributor nav rather than re-queried.
        var distributorFleetId = grn.Distributor?.FleetId;

        // 3b. Business rule: every item's product must have a configured PiecesPerPack. GRN
        //     quantities arrive in Case units from the BUSY ERP import; DistributorStock is
        //     maintained in pieces. Collect every offending product up front (not fail-fast on
        //     the first one) so the caller gets one complete, actionable error.
        var unconfigured = grn.Items.Where(i => i.Product == null || i.Product.PiecesPerPack <= 0).ToList();
        if (unconfigured.Count > 0)
            throw new BusinessRuleException(
                "GRN_PRODUCT_MISSING_PIECES_PER_PACK",
                $"Cannot confirm GRN: {unconfigured.Count} product(s) have no Pieces Per Pack configured — " +
                string.Join(", ", unconfigured.Select(i => $"{i.Product?.Code} ({i.Product?.ItemDescription})")) +
                ". Set Pieces Per Pack for these products before confirming.",
                new { grnId, products = unconfigured.Select(i => new { i.ProductId, i.Product?.Code, i.Product?.ItemDescription }) });

        // 4. Wrap in execution strategy — required because NpgsqlRetryingExecutionStrategy
        //    does not allow user-initiated transactions unless wrapped this way
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _repository.BeginTransactionAsync(ct);

            try
            {
                // 5. Update GRN status
                grn.Status          = GrnStatus.Confirmed;
                grn.ReceivedAt      = request.ReceivedAt;
                grn.ConfirmedBy     = callerId;
                grn.ConfirmedAt     = DateTime.UtcNow;
                grn.Notes           = request.Notes;
                grn.UpdatedByUserId = callerId;
                grn.UpdatedAt       = DateTime.UtcNow;

                // 6. Process each item — pessimistic locking on DistributorStock rows
                foreach (var item in grn.Items)
                {
                    var stockType = item.IsFreeIssue ? StockType.FreeIssue : StockType.Normal;

                    // Case → piece conversion. item.Quantity is the raw case count received on the
                    // invoice; DistributorStock is maintained in pieces. PiecesPerPack > 0 is
                    // guaranteed by the guard in step 3b above.
                    var piecesQuantity = item.Quantity * item.Product.PiecesPerPack;

                    // SELECT ... FOR UPDATE locks the row, preventing concurrent reads of stale QuantityOnHand
                    var stock = await _repository.GetStockForUpdateAsync(grn.DistributorId, item.ProductId, stockType, ct);

                    decimal quantityBefore;
                    if (stock is null)
                    {
                        // First stock entry for this distributor+product+stockType — create and re-lock
                        quantityBefore = 0m;
                        stock = new DistributorStock
                        {
                            DistributorId  = grn.DistributorId,
                            ProductId      = item.ProductId,
                            StockType      = stockType,
                            FleetId        = distributorFleetId,
                            QuantityOnHand = 0m,
                            LastUpdatedAt  = DateTime.UtcNow,
                        };
                        await _repository.AddStockAsync(stock, ct);
                        // Flush so the FOR UPDATE on subsequent iterations (same product+type) finds the row
                        await _repository.SaveChangesAsync(ct);

                        // Re-lock the newly created row so we hold it through the transaction
                        stock = await _repository.GetStockForUpdateAsync(grn.DistributorId, item.ProductId, stockType, ct) ?? stock;
                    }
                    else
                    {
                        quantityBefore = stock.QuantityOnHand;
                    }

                    var quantityAfter = quantityBefore + piecesQuantity;

                    // Update running balance
                    stock.QuantityOnHand = quantityAfter;
                    stock.LastUpdatedAt  = DateTime.UtcNow;

                    // Append immutable ledger entry — never update or delete
                    await _repository.AddStockTransactionAsync(new StockTransaction
                    {
                        DistributorId   = grn.DistributorId,
                        ProductId       = item.ProductId,
                        FleetId         = stock.FleetId,
                        StockType       = stockType,
                        TransactionType = StockTransactionType.GRNReceipt,
                        Direction       = StockTransactionDirection.In,
                        Quantity        = piecesQuantity,
                        QuantityBefore  = quantityBefore,
                        QuantityAfter   = quantityAfter,
                        ReferenceType   = "GRN",
                        ReferenceId     = grnId,
                        TransactedAt    = DateTime.UtcNow,
                        TransactedBy    = callerId,
                        Notes           = item.Notes,
                    }, ct);
                }

                await _repository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });

        // 7. Return updated DTO (re-fetch to get committed state)
        var confirmed = await _repository.GetGrnWithItemsReadOnlyAsync(grnId, ct)
            ?? throw new DatabaseUnavailableException();

        return ProjectToDto(confirmed);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────

    public async Task<GrnDto> GetByIdAsync(int grnId, CancellationToken ct = default)
    {
        var grn = await _repository.GetGrnWithItemsReadOnlyAsync(grnId, ct)
            ?? throw new NotFoundException("GRN", grnId);
        return ProjectToDto(grn);
    }

    // ── DTO Projection ────────────────────────────────────────────────────

    private static GrnDto ProjectToDto(GRN grn)
    {
        // GRN items are a 1:1 snapshot of the invoice's items (same ProductId, same Quantity —
        // enforced at creation, see GRN_DUPLICATE_PRODUCT guard in CreateAsync), so pricing is
        // never stored on GRNItem itself; it's always looked up from the source invoice line.
        var invoicePricesByProduct = grn.SalesInvoice?.Items
            .ToDictionary(i => i.ProductId, i => (i.UnitPrice, i.TotalPrice))
            ?? new Dictionary<int, (decimal UnitPrice, decimal TotalPrice)>();

        return new(
            grn.Id,
            grn.GrnNumber,
            grn.SalesInvoiceId,
            grn.SalesInvoice?.VchBillNo ?? string.Empty,
            grn.DistributorId,
            grn.Distributor?.Name ?? string.Empty,
            grn.Status.ToString(),
            grn.ReceivedAt,
            grn.ConfirmedBy,
            grn.ConfirmedByUser?.Name,
            grn.ConfirmedAt,
            grn.Notes,
            grn.CreatedAt,
            grn.SalesInvoice?.TotalAmount ?? 0,
            grn.Items.Select(i =>
            {
                var (unitPrice, totalPrice) = invoicePricesByProduct.TryGetValue(i.ProductId, out var price)
                    ? price
                    : (0m, 0m);
                return new GrnItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product?.ItemDescription ?? string.Empty,
                    i.Product?.Code ?? string.Empty,
                    i.Quantity,
                    i.Unit,
                    unitPrice,
                    totalPrice,
                    i.Product?.PiecesPerPack ?? 0,
                    i.Notes
                );
            }).ToList()
        );
    }
}
