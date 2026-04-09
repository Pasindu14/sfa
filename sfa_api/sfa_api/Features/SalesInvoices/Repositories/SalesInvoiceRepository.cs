using Microsoft.EntityFrameworkCore;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.SalesInvoices.Repositories;

public class SalesInvoiceRepository(AppDbContext context) : ISalesInvoiceRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Dictionary<int, int>> GetDistributorAliasDictionaryAsync(IEnumerable<int> aliases, CancellationToken ct = default)
    {
        var aliasList = aliases.ToList();
        return await _context.Distributors
            .AsNoTracking()
            .Where(d => d.IsActive && aliasList.Contains(d.Alias))
            .ToDictionaryAsync(d => d.Alias, d => d.Id, ct);
    }

    public async Task<Dictionary<string, int>> GetProductErpCodeDictionaryAsync(IEnumerable<string> erpCodes, CancellationToken ct = default)
    {
        var codeList = erpCodes.ToList();
        return await _context.Products
            .AsNoTracking()
            .IgnoreQueryFilters()   // load all — import should resolve even inactive products
            .Where(p => p.Code != null && codeList.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code!, p => p.Id, ct);
    }

    public async Task<Dictionary<string, (int Id, PurchaseOrderStatus Status)>> GetPurchaseOrderNumberDictionaryAsync(IEnumerable<string> poNumbers, CancellationToken ct = default)
    {
        var poList = poNumbers.ToList();
        return await _context.PurchaseOrders
            .AsNoTracking()
            .Where(po => poList.Contains(po.OrderNumber))
            .ToDictionaryAsync(po => po.OrderNumber, po => (po.Id, po.Status), ct);
    }

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
        DateOnly? dateFrom, DateOnly? dateTo, int? distributorId, CancellationToken ct = default)
    {
        var query = _context.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Distributor)
            .Include(x => x.ImportBatch)
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(x => EF.Functions.ILike(x.VchBillNo, pattern) ||
                                     EF.Functions.ILike(x.Distributor.Name, pattern) ||
                                     (x.SfaPoNumber != null && EF.Functions.ILike(x.SfaPoNumber, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<SalesInvoiceStatus>(status, true, out var statusEnum))
            query = query.Where(x => x.Status == statusEnum);

        if (dateFrom.HasValue)
            query = query.Where(x => x.InvoiceDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.InvoiceDate <= dateTo.Value);

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
