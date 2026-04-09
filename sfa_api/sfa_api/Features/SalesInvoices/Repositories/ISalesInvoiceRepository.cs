using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.SalesInvoices.Entities;

namespace sfa_api.Features.SalesInvoices.Repositories;

public interface ISalesInvoiceRepository
{
    // Bulk lookup helpers for import
    Task<Dictionary<int, int>> GetDistributorAliasDictionaryAsync(IEnumerable<int> aliases, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetProductErpCodeDictionaryAsync(IEnumerable<string> erpCodes, CancellationToken ct = default);
    Task<Dictionary<string, (int Id, PurchaseOrderStatus Status)>> GetPurchaseOrderNumberDictionaryAsync(IEnumerable<string> poNumbers, CancellationToken ct = default);
    Task<HashSet<string>> GetExistingVchBillNosAsync(IEnumerable<string> vchBillNosToCheck, CancellationToken ct = default);
    Task<long> GetNextBatchNumberAsync(CancellationToken ct = default);

    // Read
    Task<(List<SalesInvoice> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? search, string? status,
        DateOnly? dateFrom, DateOnly? dateTo, int? distributorId, CancellationToken ct = default);
    Task<SalesInvoice?> GetDetailAsync(int id, CancellationToken ct = default);

    // Write
    Task AddBatchAsync(SalesInvoiceImportBatch batch, CancellationToken ct = default);
    Task AddInvoicesAsync(IEnumerable<SalesInvoice> invoices, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
