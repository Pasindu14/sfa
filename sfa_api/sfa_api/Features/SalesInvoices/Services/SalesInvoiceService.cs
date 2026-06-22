using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sfa_api.Common.Errors;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.SalesInvoices.DTOs;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Features.SalesInvoices.Repositories;
using sfa_api.Features.SalesInvoices.Requests;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.SalesInvoices.Services;

public class SalesInvoiceService(
    ISalesInvoiceRepository repository,
    AppDbContext db,
    IUserRepository userRepo,
    IUserGeoAssignmentRepository geoRepo,
    IDistributorRepository distributorRepo,
    ILogger<SalesInvoiceService> logger) : ISalesInvoiceService
{
    private readonly ISalesInvoiceRepository _repository = repository;
    private readonly AppDbContext _db = db;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IUserGeoAssignmentRepository _geoRepo = geoRepo;
    private readonly IDistributorRepository _distributorRepo = distributorRepo;
    private readonly ILogger<SalesInvoiceService> _logger = logger;

    public async Task<ImportBatchResultDto> ImportAsync(
        ImportSalesInvoicesRequest request,
        int callerId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sales invoice import started: {InvoiceCount} invoice(s) in file {FileName} by caller {CallerId}",
            request.Invoices.Count, request.FileName, callerId);

        // The entire import is atomic — batch header, invoices and finalize either all
        // persist together or none do (no orphaned "Processing" batch on failure).
        // Wrapped in the execution strategy because the Npgsql retrying strategy forbids
        // user-initiated transactions unless wrapped this way.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await ImportCoreAsync(request, callerId, ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task<ImportBatchResultDto> ImportCoreAsync(
        ImportSalesInvoicesRequest request,
        int callerId,
        CancellationToken ct)
    {
        // ── Step 1: Create ImportBatch (Processing) ────────────────────────
        var seqNo = await _repository.GetNextBatchNumberAsync(ct);
        var batchNumber = $"IMP-{DateTime.UtcNow.Year}-{seqNo:D5}";
        var batch = new SalesInvoiceImportBatch
        {
            BatchNumber = batchNumber,
            FileName = request.FileName,
            Status = SalesInvoiceImportBatchStatus.Processing,
            ImportedBy = callerId,
            ImportedAt = DateTime.UtcNow
        };
        await _repository.AddBatchAsync(batch, ct);
        await _repository.SaveChangesAsync(ct);   // flush to get batch.Id

        // ── Steps 2–5: Load lookup tables into memory (scoped to batch) ──────
        var batchAliases   = request.Invoices.Select(i => i.DistributorAlias).Distinct().ToList();
        var batchErpCodes  = request.Invoices.SelectMany(i => i.Items).Select(i => i.ItemErpCode).Distinct().ToList();
        var batchPoNumbers = request.Invoices
            .Where(i => !string.IsNullOrWhiteSpace(i.SfaPoNumber))
            .Select(i => i.SfaPoNumber!)
            .Distinct()
            .ToList();
        var batchVchBillNos = request.Invoices.Select(i => i.VchBillNo).ToList();

        var distributorAliasMap = await _repository.GetDistributorAliasDictionaryAsync(batchAliases, ct);
        var productErpCodeMap   = await _repository.GetProductErpCodeDictionaryAsync(batchErpCodes, ct);
        var purchaseOrderMap    = await _repository.GetPurchaseOrderNumberDictionaryAsync(batchPoNumbers, ct);
        var existingVchBillNos  = await _repository.GetExistingVchBillNosAsync(batchVchBillNos, ct);

        // ── Step 6: Process each invoice ──────────────────────────────────
        var errors = new List<ImportBatchErrorDto>();
        var invoicesToAdd = new List<SalesInvoice>();
        var itemsToAdd    = new List<SalesInvoiceItem>();

        foreach (var inv in request.Invoices)
        {
            // a. Dedup check
            if (existingVchBillNos.Contains(inv.VchBillNo))
            {
                errors.Add(new ImportBatchErrorDto(inv.VchBillNo, "Already imported"));
                continue;
            }

            // b. Resolve distributor
            if (!distributorAliasMap.TryGetValue(inv.DistributorAlias, out var distributorId))
            {
                errors.Add(new ImportBatchErrorDto(inv.VchBillNo, $"Distributor alias {inv.DistributorAlias} not found"));
                continue;
            }

            // c. Resolve PO — must exist and be Finalized when SfaPoNumber is provided
            int? purchaseOrderId = null;
            if (!string.IsNullOrWhiteSpace(inv.SfaPoNumber))
            {
                if (!purchaseOrderMap.TryGetValue(inv.SfaPoNumber, out var po))
                {
                    errors.Add(new ImportBatchErrorDto(inv.VchBillNo, $"SFA PO number '{inv.SfaPoNumber}' not found"));
                    continue;
                }
                if (po.Status != PurchaseOrderStatus.Finalized)
                {
                    errors.Add(new ImportBatchErrorDto(inv.VchBillNo, $"SFA PO '{inv.SfaPoNumber}' is not acknowledged (status: {po.Status})"));
                    continue;
                }
                purchaseOrderId = po.Id;
            }

            // d. Resolve items — skip invoice if any product is unresolvable
            // If ANY item in the voucher is free issue, ALL items are free issue
            // (BUSY ERP only marks the header row with Y; continuation rows are blank)
            var voucherIsFreeIssue = inv.Items.Any(i => i.IsFreeIssue);
            var itemEntities = new List<SalesInvoiceItem>();
            var itemError = false;
            foreach (var item in inv.Items)
            {
                if (!productErpCodeMap.TryGetValue(item.ItemErpCode, out var productId))
                {
                    errors.Add(new ImportBatchErrorDto(inv.VchBillNo, $"Product ERP code '{item.ItemErpCode}' not found"));
                    itemError = true;
                    break;
                }
                itemEntities.Add(new SalesInvoiceItem
                {
                    ProductId       = productId,
                    ItemErpCode     = item.ItemErpCode,
                    ItemDescription = item.ItemDescription,
                    Quantity        = item.Quantity,
                    Unit            = item.Unit,
                    UnitPrice       = item.UnitPrice,
                    TotalPrice      = item.TotalPrice,
                    IsFreeIssue     = voucherIsFreeIssue,
                    LineNumber      = item.LineNumber
                });
            }
            if (itemError) continue;

            // d2. Money-math validation — never trust the client's totals blindly.
            const decimal tolerance = 0.01m;
            var badLine = itemEntities.FirstOrDefault(
                it => Math.Abs(it.TotalPrice - Math.Round(it.Quantity * it.UnitPrice, 2)) > tolerance);
            if (badLine is not null)
            {
                errors.Add(new ImportBatchErrorDto(inv.VchBillNo,
                    $"Line total mismatch for '{badLine.ItemErpCode}': {badLine.TotalPrice} != {badLine.Quantity} × {badLine.UnitPrice}"));
                continue;
            }
            // Header total must reconcile with the sum of lines (free-issue vouchers carry
            // notional line values that don't roll up to the header, so skip them).
            if (!voucherIsFreeIssue)
            {
                var lineSum = itemEntities.Sum(it => it.TotalPrice);
                if (Math.Abs(inv.TotalAmount - lineSum) > tolerance * Math.Max(1, itemEntities.Count))
                {
                    errors.Add(new ImportBatchErrorDto(inv.VchBillNo,
                        $"Invoice total {inv.TotalAmount} does not match sum of lines {lineSum}"));
                    continue;
                }
            }

            // e. Build invoice entity — if any item is free issue the whole voucher is FreeIssue
            Enum.TryParse<SalesInvoiceType>(inv.InvoiceType, out var invoiceType);
            if (voucherIsFreeIssue) invoiceType = SalesInvoiceType.FreeIssue;
            var invoice = new SalesInvoice
            {
                VchBillNo           = inv.VchBillNo,
                BusyOrderRequestNo  = inv.BusyOrderRequestNo,
                SfaPoNumber         = inv.SfaPoNumber,
                PurchaseOrderId     = purchaseOrderId,
                DistributorId       = distributorId,
                InvoiceDate         = inv.InvoiceDate,
                InvoiceType         = invoiceType,
                TotalAmount         = inv.TotalAmount,
                ImportBatchId       = batch.Id,
                Status              = SalesInvoiceStatus.Pending
            };
            invoicesToAdd.Add(invoice);

            // Link items to invoice — they'll be associated via EF navigation
            foreach (var item in itemEntities)
                invoice.Items.Add(item);

            // Track for dedup within the same file
            existingVchBillNos.Add(inv.VchBillNo);
        }

        // ── Step 7: Bulk insert ────────────────────────────────────────────
        if (invoicesToAdd.Count > 0)
        {
            await _repository.AddInvoicesAsync(invoicesToAdd, ct);
            await _repository.SaveChangesAsync(ct);
        }

        // ── Step 8: Finalize batch ────────────────────────────────────────
        var importedCount = invoicesToAdd.Count;
        var skippedCount  = request.Invoices.Count - importedCount;
        var totalItems    = invoicesToAdd.Sum(i => i.Items.Count);
        var totalAmount   = invoicesToAdd.Sum(i => i.TotalAmount);

        var finalStatus = skippedCount == 0
            ? SalesInvoiceImportBatchStatus.Completed
            : importedCount == 0
                ? SalesInvoiceImportBatchStatus.Failed
                : SalesInvoiceImportBatchStatus.PartialFailed;

        batch.TotalInvoices = request.Invoices.Count;
        batch.TotalItems    = totalItems;
        batch.TotalAmount   = totalAmount;
        batch.Status        = finalStatus;
        batch.ErrorSummary  = errors.Count > 0
            ? JsonSerializer.Serialize(errors)
            : null;
        batch.UpdatedAt = DateTime.UtcNow;

        if (finalStatus == SalesInvoiceImportBatchStatus.PartialFailed)
            _logger.LogWarning(
                "Sales invoice import {BatchNumber} completed with partial failures: " +
                "{ImportedCount} imported, {SkippedCount} skipped out of {TotalCount}",
                batchNumber, importedCount, skippedCount, request.Invoices.Count);
        else
            _logger.LogInformation(
                "Sales invoice import {BatchNumber} completed: status={Status}, " +
                "imported={ImportedCount}, items={TotalItems}, amount={TotalAmount}",
                batchNumber, finalStatus, importedCount, totalItems, totalAmount);

        await _repository.SaveChangesAsync(ct);

        return new ImportBatchResultDto(
            batch.Id,
            batch.BatchNumber,
            request.Invoices.Count,
            importedCount,
            skippedCount,
            totalItems,
            totalAmount,
            finalStatus.ToString(),
            errors);
    }

    // ── Read ──────────────────────────────────────────────────────────────

    // Resolves the distributor a non-management caller is allowed to see (audit finding #4).
    //   (Restricted=true,  id): caller may only see invoices for distributor `id`
    //                           (id may be null = "matches nothing", e.g. a rep with no territory).
    //   (Restricted=false, _ ): management / Admin — unrestricted.
    private async Task<(bool Restricted, int? DistributorId)> ResolveDistributorScopeAsync(
        int callerId, UserRole callerRole, CancellationToken ct)
    {
        switch (callerRole)
        {
            case UserRole.Distributor:
                var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                    ?? throw new NotFoundException("User", callerId);
                return (true, caller.DistributorId);
            case UserRole.SalesRep:
                var geo = await _geoRepo.GetActiveByUserIdAsync(callerId, ct);
                int? distId = null;
                if (geo?.TerritoryId is int territoryId)
                    distId = (await _distributorRepo.GetByTerritoryIdAsync(territoryId, ct))?.Id;
                return (true, distId);
            default:
                return (false, null);
        }
    }

    public async Task<(List<SalesInvoiceListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? search, string? status,
        DateOnly? dateFrom, DateOnly? dateTo, int? distributorId,
        int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        // Non-management callers are pinned to their own distributor scope; any client-supplied
        // distributorId is ignored for them. Management/Admin keep the optional distributorId filter.
        var (restricted, scopeDistributorId) = await ResolveDistributorScopeAsync(callerId, callerRole, ct);
        if (restricted)
        {
            if (scopeDistributorId is null)
                return ([], 0);
            distributorId = scopeDistributorId;
        }

        var (invoices, total) = await _repository.GetListAsync(page, pageSize, search, status, dateFrom, dateTo, distributorId, ct);
        var dtos = invoices.Select(inv => new SalesInvoiceListDto(
            inv.Id,
            inv.VchBillNo,
            inv.BusyOrderRequestNo,
            inv.SfaPoNumber,
            inv.DistributorId,
            inv.Distributor?.Name ?? string.Empty,
            inv.InvoiceDate.ToString("yyyy-MM-dd"),
            inv.InvoiceType.ToString(),
            inv.Items.Any(i => i.IsFreeIssue),
            inv.TotalAmount,
            inv.Status.ToString(),
            inv.ImportBatch?.BatchNumber ?? string.Empty,
            inv.CreatedAt
        )).ToList();
        return (dtos, total);
    }

    public async Task<SalesInvoiceDetailDto> GetDetailAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var inv = await _repository.GetDetailAsync(id, ct)
            ?? throw new NotFoundException("SalesInvoice", id);

        // A Distributor / SalesRep may only read invoices within their own distributor scope;
        // managers / Admin may read any (audit finding #4).
        var (restricted, scopeDistributorId) = await ResolveDistributorScopeAsync(callerId, callerRole, ct);
        if (restricted && inv.DistributorId != scopeDistributorId)
            throw new AuthorizationException("this sales invoice");

        return new SalesInvoiceDetailDto(
            inv.Id,
            inv.VchBillNo,
            inv.BusyOrderRequestNo,
            inv.SfaPoNumber,
            inv.PurchaseOrderId,
            inv.DistributorId,
            inv.Distributor?.Name ?? string.Empty,
            inv.InvoiceDate.ToString("yyyy-MM-dd"),
            inv.InvoiceType.ToString(),
            inv.Items.Any(i => i.IsFreeIssue),
            inv.TotalAmount,
            inv.Status.ToString(),
            inv.ImportBatchId,
            inv.ImportBatch?.BatchNumber ?? string.Empty,
            inv.CreatedAt,
            inv.Items.OrderBy(i => i.LineNumber).Select(i => new SalesInvoiceItemDto(
                i.Id,
                i.ProductId,
                i.Product?.Code ?? string.Empty,
                i.ItemErpCode,
                i.ItemDescription,
                i.Quantity,
                i.Unit,
                i.UnitPrice,
                i.TotalPrice,
                i.IsFreeIssue,
                i.LineNumber
            )).ToList()
        );
    }
}
