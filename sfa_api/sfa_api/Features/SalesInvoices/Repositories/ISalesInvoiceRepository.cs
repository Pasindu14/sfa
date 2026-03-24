using sfa_api.Features.SalesInvoices.Entities;

namespace sfa_api.Features.SalesInvoices.Repositories;

public interface ISalesInvoiceRepository
{
    // Bulk lookup helpers for import
    Task<Dictionary<int, int>> GetDistributorAliasDictionaryAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetProductErpCodeDictionaryAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetPurchaseOrderNumberDictionaryAsync(CancellationToken ct = default);
    Task<HashSet<string>> GetExistingVchBillNosAsync(CancellationToken ct = default);
    Task<long> GetNextBatchNumberAsync(CancellationToken ct = default);

    // Write
    Task AddBatchAsync(SalesInvoiceImportBatch batch, CancellationToken ct = default);
    Task AddInvoicesAsync(IEnumerable<SalesInvoice> invoices, CancellationToken ct = default);
    Task AddItemsAsync(IEnumerable<SalesInvoiceItem> items, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
