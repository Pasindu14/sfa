using sfa_api.Features.SalesOrders.Entities;
using sfa_api.Features.SalesOrders.Enums;

namespace sfa_api.Features.SalesOrders.Repositories;

public interface ISalesOrderRepository
{
    Task<SalesOrder?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SalesOrder?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<SalesOrder> SalesOrders, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        SalesOrderStatus? status = null,
        int? distributorId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);
    Task<long> GetNextOrderNumberAsync(CancellationToken ct = default);
    Task CreateAsync(SalesOrder order, CancellationToken ct = default);
    Task UpdateAsync(SalesOrder order, CancellationToken ct = default);
    Task AddItemsAsync(IEnumerable<SalesOrderItem> items, CancellationToken ct = default);
    Task RemoveItemsAsync(int salesOrderId, CancellationToken ct = default);
    Task AddHistoryAsync(SalesOrderHistory history, CancellationToken ct = default);
    Task<IEnumerable<SalesOrderHistory>> GetHistoryAsync(int salesOrderId, CancellationToken ct = default);
    Task<Dictionary<SalesOrderStatus, int>> GetCountsByStatusAsync(
        int? distributorId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
