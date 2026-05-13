using sfa_api.Features.PurchaseOrders.Entities;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.PurchaseOrders.Repositories;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only IPurchaseOrderRepository wrapper that replaces GetNextOrderNumberAsync
/// (which calls PostgreSQL nextval()) with an in-process atomic counter.
/// All other calls delegate to the real PurchaseOrderRepository.
/// </summary>
public sealed class TestPurchaseOrderRepository(IPurchaseOrderRepository inner) : IPurchaseOrderRepository
{
    private static long _counter = 0;

    public Task<long> GetNextOrderNumberAsync(CancellationToken ct = default)
        => Task.FromResult(Interlocked.Increment(ref _counter));

    public Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default)
        => inner.GetByIdAsync(id, ct);

    public Task<PurchaseOrder?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => inner.GetByIdWithItemsAsync(id, ct);

    public Task<(IEnumerable<PurchaseOrder> PurchaseOrders, int TotalCount)> GetAllAsync(
        int skip, int take, string? search = null,
        PurchaseOrderStatus? status = null, int? distributorId = null,
        DateOnly? fromDate = null, DateOnly? toDate = null,
        CancellationToken ct = default)
        => inner.GetAllAsync(skip, take, search, status, distributorId, fromDate, toDate, ct);

    public Task CreateAsync(PurchaseOrder order, CancellationToken ct = default)
        => inner.CreateAsync(order, ct);

    public Task UpdateAsync(PurchaseOrder order, CancellationToken ct = default)
        => inner.UpdateAsync(order, ct);

    public Task AddItemsAsync(IEnumerable<PurchaseOrderItem> items, CancellationToken ct = default)
        => inner.AddItemsAsync(items, ct);

    public Task RemoveItemsAsync(int purchaseOrderId, CancellationToken ct = default)
        => inner.RemoveItemsAsync(purchaseOrderId, ct);

    public Task AddHistoryAsync(PurchaseOrderHistory history, CancellationToken ct = default)
        => inner.AddHistoryAsync(history, ct);

    public Task<IEnumerable<PurchaseOrderHistory>> GetHistoryAsync(int purchaseOrderId, CancellationToken ct = default)
        => inner.GetHistoryAsync(purchaseOrderId, ct);

    public Task<Dictionary<PurchaseOrderStatus, int>> GetCountsByStatusAsync(
        int? distributorId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default)
        => inner.GetCountsByStatusAsync(distributorId, fromDate, toDate, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
