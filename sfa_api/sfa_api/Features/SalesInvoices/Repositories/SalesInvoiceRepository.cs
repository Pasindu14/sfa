using Microsoft.EntityFrameworkCore;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Enums;
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

    public async Task<Dictionary<string, (int Id, PurchaseOrderStatus Status)>> GetPurchaseOrderNumberDictionaryAsync(CancellationToken ct = default)
        => await _context.PurchaseOrders
            .AsNoTracking()
            .ToDictionaryAsync(po => po.OrderNumber, po => (po.Id, po.Status), ct);

    public async Task<HashSet<string>> GetExistingVchBillNosAsync(IEnumerable<string> vchBillNosToCheck, CancellationToken ct = default)
    {
        var toCheck = vchBillNosToCheck.ToList();
        var nos = await _context.SalesInvoices
            .AsNoTracking()
            .Where(si => toCheck.Contains(si.VchBillNo))
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

    public async Task<(List<SalesInvoice> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? search, string? status,
        DateOnly? date, int? distributorId, CancellationToken ct = default)
    {
        var query = _context.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Distributor)
            .Include(x => x.ImportBatch)
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.VchBillNo.Contains(search) ||
                                     x.Distributor.Name.Contains(search) ||
                                     (x.SfaPoNumber != null && x.SfaPoNumber.Contains(search)));

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<SalesInvoiceStatus>(status, true, out var statusEnum))
            query = query.Where(x => x.Status == statusEnum);

        if (date.HasValue)
            query = query.Where(x => x.InvoiceDate == date.Value);

        if (distributorId.HasValue)
            query = query.Where(x => x.DistributorId == distributorId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<SalesInvoice?> GetDetailAsync(int id, CancellationToken ct = default)
        => _context.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Distributor)
            .Include(x => x.ImportBatch)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

    public async Task AddBatchAsync(SalesInvoiceImportBatch batch, CancellationToken ct = default)
        => await _context.SalesInvoiceImportBatches.AddAsync(batch, ct);

    public async Task AddInvoicesAsync(IEnumerable<SalesInvoice> invoices, CancellationToken ct = default)
        => await _context.SalesInvoices.AddRangeAsync(invoices, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
