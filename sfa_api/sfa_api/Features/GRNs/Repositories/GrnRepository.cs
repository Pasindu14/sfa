using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.GRNs.Entities;
using sfa_api.Features.GRNs.Enums;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.GRNs.Repositories;

public class GrnRepository(AppDbContext db) : IGrnRepository
{
    private readonly AppDbContext _db = db;

    // ── SalesInvoice ──────────────────────────────────────────────────────

    public Task<SalesInvoice?> GetSalesInvoiceWithItemsAsync(int salesInvoiceId, CancellationToken ct = default)
        => _db.SalesInvoices
              .Include(x => x.Items)
              .FirstOrDefaultAsync(x => x.Id == salesInvoiceId && x.IsActive, ct);

    // ── GRN list ──────────────────────────────────────────────────────────

    public async Task<(List<GRN> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? status, int? distributorId, DateOnly? dateFrom = null, DateOnly? dateTo = null, CancellationToken ct = default)
    {
        var query = _db.GRNs
            .AsNoTracking()
            .Include(x => x.SalesInvoice)
            .Include(x => x.Distributor)
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<GrnStatus>(status, true, out var statusEnum))
            query = query.Where(x => x.Status == statusEnum);

        if (distributorId.HasValue)
            query = query.Where(x => x.DistributorId == distributorId.Value);

        if (dateFrom.HasValue)
        {
            var start = DateTime.SpecifyKind(dateFrom.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt >= start);
        }

        if (dateTo.HasValue)
        {
            var end = DateTime.SpecifyKind(dateTo.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt <= end);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // ── GRN read ──────────────────────────────────────────────────────────

    public Task<GRN?> GetGrnWithItemsAsync(int grnId, CancellationToken ct = default)
        => _db.GRNs
              .Include(x => x.Items)
                  .ThenInclude(i => i.Product)
              .Include(x => x.SalesInvoice)
              .Include(x => x.Distributor)
              .Include(x => x.ConfirmedByUser)
              .FirstOrDefaultAsync(x => x.Id == grnId && x.IsActive, ct);

    public Task<GRN?> GetGrnWithItemsReadOnlyAsync(int grnId, CancellationToken ct = default)
        => _db.GRNs
              .AsNoTracking()
              .Include(x => x.Items)
                  .ThenInclude(i => i.Product)
              .Include(x => x.SalesInvoice)
              .Include(x => x.Distributor)
              .Include(x => x.ConfirmedByUser)
              .FirstOrDefaultAsync(x => x.Id == grnId && x.IsActive, ct);

    public Task<bool> GrnExistsForInvoiceAsync(int salesInvoiceId, CancellationToken ct = default)
        => _db.GRNs.AnyAsync(x => x.SalesInvoiceId == salesInvoiceId, ct);

    // ── GRN sequence ──────────────────────────────────────────────────────

    public async Task<long> GetNextGrnNumberAsync(CancellationToken ct = default)
    {
        var result = await _db.Database
            .SqlQueryRaw<long>("SELECT nextval('grn_number_seq')")
            .ToListAsync(ct);
        return result[0];
    }

    // ── GRN write ─────────────────────────────────────────────────────────

    public Task AddGrnAsync(GRN grn, CancellationToken ct = default)
    {
        _db.GRNs.Add(grn);
        return Task.CompletedTask;
    }

    // ── Stock ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Issues a raw SELECT ... FOR UPDATE to pessimistically lock the row.
    /// The EF-tracked entity is returned so updates flow through the change tracker.
    /// Must be called within a transaction.
    /// </summary>
    public async Task<DistributorStock?> GetStockForUpdateAsync(
        int distributorId, int productId, CancellationToken ct = default)
    {
        // Raw SQL to get the row ID with a FOR UPDATE lock
        var ids = await _db.Database
            .SqlQueryRaw<int>(
                "SELECT \"Id\" FROM \"DistributorStocks\" WHERE \"DistributorId\" = {0} AND \"ProductId\" = {1} FOR UPDATE",
                distributorId, productId)
            .ToListAsync(ct);

        if (ids.Count == 0) return null;

        // Fetch through EF so the entity is tracked (change tracking handles the UPDATE)
        return await _db.DistributorStocks
            .FirstOrDefaultAsync(x => x.Id == ids[0], ct);
    }

    public Task AddStockAsync(DistributorStock stock, CancellationToken ct = default)
    {
        _db.DistributorStocks.Add(stock);
        return Task.CompletedTask;
    }

    public Task AddStockTransactionAsync(StockTransaction tx, CancellationToken ct = default)
    {
        _db.StockTransactions.Add(tx);
        return Task.CompletedTask;
    }

    // ── Persistence ───────────────────────────────────────────────────────

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _db.Database.BeginTransactionAsync(ct);
}
