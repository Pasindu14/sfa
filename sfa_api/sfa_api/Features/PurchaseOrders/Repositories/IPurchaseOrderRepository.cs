using sfa_api.Features.PurchaseOrders.Entities;
using sfa_api.Features.PurchaseOrders.Enums;

namespace sfa_api.Features.PurchaseOrders.Repositories;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PurchaseOrder?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<PurchaseOrder> PurchaseOrders, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        PurchaseOrderStatus? status = null,
        IEnumerable<int>? distributorIds = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);
    Task<long> GetNextOrderNumberAsync(CancellationToken ct = default);
    Task CreateAsync(PurchaseOrder order, CancellationToken ct = default);
    Task UpdateAsync(PurchaseOrder order, CancellationToken ct = default);
    Task AddItemsAsync(IEnumerable<PurchaseOrderItem> items, CancellationToken ct = default);
    Task RemoveItemsAsync(int purchaseOrderId, CancellationToken ct = default);
    Task AddHistoryAsync(PurchaseOrderHistory history, CancellationToken ct = default);
    Task<IEnumerable<PurchaseOrderHistory>> GetHistoryAsync(int purchaseOrderId, CancellationToken ct = default);
    Task<Dictionary<PurchaseOrderStatus, int>> GetCountsByStatusAsync(
        int? distributorId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
