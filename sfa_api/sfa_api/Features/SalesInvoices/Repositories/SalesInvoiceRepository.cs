using Microsoft.EntityFrameworkCore;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.SalesInvoices.Repositories;

public class SalesInvoiceRepository(AppDbContext context) : ISalesInvoiceRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Dictionary<int, int>> GetDistributorAliasDictionaryAsync(CancellationToken ct = default)
        => await _context.Distributors
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToDictionaryAsync(d => d.Alias, d => d.Id, ct);

    public async Task<Dictionary<string, int>> GetProductErpCodeDictionaryAsync(CancellationToken ct = default)
        => await _context.Products
            .AsNoTracking()
            .IgnoreQueryFilters()   // load all — import should resolve even inactive products
            .Where(p => p.Code != null)
            .ToDictionaryAsync(p => p.Code!, p => p.Id, ct);

    public async Task<Dictionary<string, int>> GetPurchaseOrderNumberDictionaryAsync(CancellationToken ct = default)
        => await _context.PurchaseOrders
            .AsNoTracking()
            .ToDictionaryAsync(po => po.OrderNumber, po => po.Id, ct);

    public async Task<HashSet<string>> GetExistingVchBillNosAsync(CancellationToken ct = default)
    {
        var nos = await _context.SalesInvoices
            .AsNoTracking()
            .Select(si => si.VchBillNo)
            .ToListAsync(ct);
        return [.. nos];
    }

    public async Task<long> GetNextBatchNumberAsync(CancellationToken ct = default)
    {
        var result = await _context.Database
            .SqlQueryRaw<long>("SELECT nextval('sales_invoice_import_batch_number_seq') AS \"Value\"")
            .FirstAsync(ct);
        return result;
    }

    public async Task AddBatchAsync(SalesInvoiceImportBatch batch, CancellationToken ct = default)
        => await _context.SalesInvoiceImportBatches.AddAsync(batch, ct);

    public async Task AddInvoicesAsync(IEnumerable<SalesInvoice> invoices, CancellationToken ct = default)
        => await _context.SalesInvoices.AddRangeAsync(invoices, ct);

    public async Task AddItemsAsync(IEnumerable<SalesInvoiceItem> items, CancellationToken ct = default)
        => await _context.SalesInvoiceItems.AddRangeAsync(items, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
