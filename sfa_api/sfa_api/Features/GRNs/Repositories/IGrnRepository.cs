using sfa_api.Features.GRNs.Entities;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.Stock.Entities;

namespace sfa_api.Features.GRNs.Repositories;

public interface IGrnRepository
{
    // ── SalesInvoice lookups ───────────────────────────────────────────────
    Task<SalesInvoice?> GetSalesInvoiceWithItemsAsync(int salesInvoiceId, CancellationToken ct = default);

    // ── GRN read ───────────────────────────────────────────────────────────
    Task<(List<GRN> Items, int TotalCount)> GetListAsync(int page, int pageSize, string? status, int? distributorId, DateOnly? date = null, CancellationToken ct = default);
    /// <summary>Tracked — use when you will mutate the returned entity (e.g. ConfirmAsync).</summary>
    Task<GRN?> GetGrnWithItemsAsync(int grnId, CancellationToken ct = default);
    /// <summary>AsNoTracking — use for read-only projections (GET endpoints, post-save reloads).</summary>
    Task<GRN?> GetGrnWithItemsReadOnlyAsync(int grnId, CancellationToken ct = default);
    Task<bool> GrnExistsForInvoiceAsync(int salesInvoiceId, CancellationToken ct = default);

    // ── GRN sequence ───────────────────────────────────────────────────────
    Task<long> GetNextGrnNumberAsync(CancellationToken ct = default);

    // ── GRN write ─────────────────────────────────────────────────────────
    Task AddGrnAsync(GRN grn, CancellationToken ct = default);

    // ── Stock (pessimistic locked read) ────────────────────────────────────
    /// <summary>
    /// Returns the DistributorStock row locked with SELECT FOR UPDATE.
    /// Must be called inside an explicit transaction.
    /// Returns null if no row exists yet (caller should create one).
    /// </summary>
    Task<DistributorStock?> GetStockForUpdateAsync(int distributorId, int productId, CancellationToken ct = default);
    Task AddStockAsync(DistributorStock stock, CancellationToken ct = default);
    Task AddStockTransactionAsync(sfa_api.Features.Stock.Entities.StockTransaction tx, CancellationToken ct = default);

    // ── Persistence ───────────────────────────────────────────────────────
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
