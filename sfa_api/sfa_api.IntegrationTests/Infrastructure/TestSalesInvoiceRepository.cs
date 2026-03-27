using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Repositories;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only ISalesInvoiceRepository wrapper that replaces GetNextBatchNumberAsync
/// (which calls PostgreSQL nextval()) with an in-process atomic counter.
/// All other calls delegate to the real SalesInvoiceRepository.
/// </summary>
public sealed class TestSalesInvoiceRepository(ISalesInvoiceRepository inner) : ISalesInvoiceRepository
{
    private static long _counter = 0;

    public Task<long> GetNextBatchNumberAsync(CancellationToken ct = default)
        => Task.FromResult(Interlocked.Increment(ref _counter));

    public Task<Dictionary<int, int>> GetDistributorAliasDictionaryAsync(IEnumerable<int> aliases, CancellationToken ct = default)
        => inner.GetDistributorAliasDictionaryAsync(aliases, ct);

    public Task<Dictionary<string, int>> GetProductErpCodeDictionaryAsync(IEnumerable<string> erpCodes, CancellationToken ct = default)
        => inner.GetProductErpCodeDictionaryAsync(erpCodes, ct);

    public Task<Dictionary<string, (int Id, PurchaseOrderStatus Status)>> GetPurchaseOrderNumberDictionaryAsync(IEnumerable<string> poNumbers, CancellationToken ct = default)
        => inner.GetPurchaseOrderNumberDictionaryAsync(poNumbers, ct);

    public Task<HashSet<string>> GetExistingVchBillNosAsync(IEnumerable<string> vchBillNosToCheck, CancellationToken ct = default)
        => inner.GetExistingVchBillNosAsync(vchBillNosToCheck, ct);

    public Task<(List<SalesInvoice> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? search, string? status,
        DateOnly? date, int? distributorId, CancellationToken ct = default)
        => inner.GetListAsync(page, pageSize, search, status, date, distributorId, ct);

    public Task<SalesInvoice?> GetDetailAsync(int id, CancellationToken ct = default)
        => inner.GetDetailAsync(id, ct);

    public Task AddBatchAsync(SalesInvoiceImportBatch batch, CancellationToken ct = default)
        => inner.AddBatchAsync(batch, ct);

    public Task AddInvoicesAsync(IEnumerable<SalesInvoice> invoices, CancellationToken ct = default)
        => inner.AddInvoicesAsync(invoices, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
