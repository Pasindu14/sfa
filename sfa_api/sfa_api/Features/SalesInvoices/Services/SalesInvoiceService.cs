using System.Text.Json;
using sfa_api.Common.Errors;
using sfa_api.Features.SalesInvoices.DTOs;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Features.SalesInvoices.Repositories;
using sfa_api.Features.SalesInvoices.Requests;

namespace sfa_api.Features.SalesInvoices.Services;

public class SalesInvoiceService(ISalesInvoiceRepository repository) : ISalesInvoiceService
{
    private readonly ISalesInvoiceRepository _repository = repository;

    public async Task<ImportBatchResultDto> ImportAsync(
        ImportSalesInvoicesRequest request,
        int callerId,
        CancellationToken ct = default)
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

        // ── Steps 2–5: Load lookup tables into memory ──────────────────────
        var distributorAliasMap = await _repository.GetDistributorAliasDictionaryAsync(ct);
        var productErpCodeMap   = await _repository.GetProductErpCodeDictionaryAsync(ct);
        var purchaseOrderMap    = await _repository.GetPurchaseOrderNumberDictionaryAsync(ct);
        var existingVchBillNos  = await _repository.GetExistingVchBillNosAsync(ct);

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

            // c. Resolve PO (nullable)
            int? purchaseOrderId = null;
            if (!string.IsNullOrWhiteSpace(inv.SfaPoNumber) &&
                purchaseOrderMap.TryGetValue(inv.SfaPoNumber, out var resolvedPoId))
                purchaseOrderId = resolvedPoId;

            // d. Resolve items — skip invoice if any product is unresolvable
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
                    IsFreeIssue     = item.IsFreeIssue,
                    LineNumber      = item.LineNumber
                });
            }
            if (itemError) continue;

            // e. Build invoice entity
            Enum.TryParse<SalesInvoiceType>(inv.InvoiceType, out var invoiceType);
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

    public async Task<(List<SalesInvoiceListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default)
    {
        var (invoices, total) = await _repository.GetListAsync(page, pageSize, search, status, ct);
        var dtos = invoices.Select(inv => new SalesInvoiceListDto(
            inv.Id,
            inv.VchBillNo,
            inv.BusyOrderRequestNo,
            inv.SfaPoNumber,
            inv.DistributorId,
            inv.Distributor?.Name ?? string.Empty,
            inv.InvoiceDate.ToString("yyyy-MM-dd"),
            inv.InvoiceType.ToString(),
            inv.TotalAmount,
            inv.Status.ToString(),
            inv.ImportBatch?.BatchNumber ?? string.Empty,
            inv.CreatedAt
        )).ToList();
        return (dtos, total);
    }

    public async Task<SalesInvoiceDetailDto> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var inv = await _repository.GetDetailAsync(id, ct)
            ?? throw new NotFoundException("SalesInvoice", id);

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
