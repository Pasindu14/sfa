using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Common.Extensions;
using sfa_api.Features.GRNs.Entities;
using sfa_api.Features.GRNs.Enums;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.GRNs.Repositories;

public class GrnRepository(AppDbContext db) : IGrnRepository
{
    private readonly AppDbContext _db = db;

    private const string LikeEscapeChar = "\\";

    private static string EscapeLikePattern(string input)
        => input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // ── SalesInvoice ──────────────────────────────────────────────────────

    public Task<SalesInvoice?> GetSalesInvoiceWithItemsAsync(int salesInvoiceId, CancellationToken ct = default)
        => _db.SalesInvoices
              .Include(x => x.Items)
              .FirstOrDefaultAsync(x => x.Id == salesInvoiceId && x.IsActive, ct);

    // ── GRN list ──────────────────────────────────────────────────────────

    public async Task<(List<GRN> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? status, int? distributorId, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? search = null, CancellationToken ct = default)
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
            // Sri Lankan midnight, not UTC midnight — otherwise the window runs
            // 05:30→05:30 SL and GRNs received before dawn land on the previous day.
            var start = SriLankaTime.StartOfDayUtc(dateFrom.Value);
            query = query.Where(x => x.CreatedAt >= start);
        }

        if (dateTo.HasValue)
        {
            // Half-open, matching PurchaseOrderRepository. dateTo is still fully included;
            // the previous `<= TimeOnly.MaxValue` form let a record on the exact boundary
            // tick belong to two adjacent ranges at once.
            var endExclusive = SriLankaTime.StartOfDayUtc(dateTo.Value.AddDays(1));
            query = query.Where(x => x.CreatedAt < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Case-insensitive substring match, same provider switch as SalesInvoiceRepository:
            // ILIKE on Postgres rides the pg_trgm GIN indexes (IX_GRNs_GrnNumber_Trgm,
            // IX_SalesInvoices_VchBillNo_Trgm); SQLite (tests) has no ILIKE but its LIKE is
            // already ASCII case-insensitive. LIKE metacharacters in the user's input are escaped
            // so "%" / "_" still match literally, as they did under the previous Contains().
            var pattern = $"%{EscapeLikePattern(search)}%";
            query = _db.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(x =>
                    EF.Functions.ILike(x.GrnNumber, pattern, LikeEscapeChar) ||
                    (x.SalesInvoice != null && EF.Functions.ILike(x.SalesInvoice.VchBillNo, pattern, LikeEscapeChar)))
                : query.Where(x =>
                    EF.Functions.Like(x.GrnNumber, pattern, LikeEscapeChar) ||
                    (x.SalesInvoice != null && EF.Functions.Like(x.SalesInvoice.VchBillNo, pattern, LikeEscapeChar)));
        }

        var (_, size, skip) = PaginationHelper.Normalize(page, pageSize);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(size)
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
                  .ThenInclude(si => si.Items)
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
    /// One raw SELECT … ORDER BY "Id" FOR UPDATE over all requested rows; the EF-tracked entities
    /// are returned so updates flow through the change tracker. Must be called within a transaction.
    /// </summary>
    public Task<Dictionary<sfa_api.Features.Stock.Repositories.StockKey, DistributorStock>> LockStocksForUpdateAsync(
        IEnumerable<sfa_api.Features.Stock.Repositories.StockKey> keys, CancellationToken ct = default)
        => sfa_api.Features.Stock.Repositories.StockLocking.LockForUpdateAsync(_db, keys, ct);

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
