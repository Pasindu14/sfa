using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.GRNs.Entities;
using sfa_api.Features.GRNs.Repositories;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only IGrnRepository wrapper that replaces GetNextGrnNumberAsync
/// (which calls PostgreSQL nextval()) with an in-process atomic counter.
/// All other calls delegate to the real GrnRepository.
/// </summary>
public sealed class TestGrnRepository(IGrnRepository inner, AppDbContext db) : IGrnRepository
{
    private static long _counter = 0;

    public Task<long> GetNextGrnNumberAsync(CancellationToken ct = default)
        => Task.FromResult(Interlocked.Increment(ref _counter));

    public Task<SalesInvoice?> GetSalesInvoiceWithItemsAsync(int salesInvoiceId, CancellationToken ct = default)
        => inner.GetSalesInvoiceWithItemsAsync(salesInvoiceId, ct);

    public Task<(List<GRN> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? status, int? distributorId, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? search = null, CancellationToken ct = default)
        => inner.GetListAsync(page, pageSize, status, distributorId, dateFrom, dateTo, search, ct);

    public Task<GRN?> GetGrnWithItemsAsync(int grnId, CancellationToken ct = default)
        => inner.GetGrnWithItemsAsync(grnId, ct);

    public Task<GRN?> GetGrnWithItemsReadOnlyAsync(int grnId, CancellationToken ct = default)
        => inner.GetGrnWithItemsReadOnlyAsync(grnId, ct);

    public Task<bool> GrnExistsForInvoiceAsync(int salesInvoiceId, CancellationToken ct = default)
        => inner.GrnExistsForInvoiceAsync(salesInvoiceId, ct);

    public Task AddGrnAsync(GRN grn, CancellationToken ct = default)
        => inner.AddGrnAsync(grn, ct);

    // Batched lock: SQLite has no FOR UPDATE / unnest, so load the rows (tracked) with plain LINQ instead.
    public Task<Dictionary<StockKey, DistributorStock>> LockStocksForUpdateAsync(
        IEnumerable<StockKey> keys, CancellationToken ct = default)
        => TestStockLocking.LoadAsync(db, keys, ct);

    public Task AddStockAsync(DistributorStock stock, CancellationToken ct = default)
        => inner.AddStockAsync(stock, ct);

    public Task AddStockTransactionAsync(StockTransaction tx, CancellationToken ct = default)
        => inner.AddStockTransactionAsync(tx, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => inner.BeginTransactionAsync(ct);
}
