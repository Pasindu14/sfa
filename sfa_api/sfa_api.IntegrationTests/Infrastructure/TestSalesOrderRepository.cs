using sfa_api.Features.SalesOrders.Entities;
using sfa_api.Features.SalesOrders.Enums;
using sfa_api.Features.SalesOrders.Repositories;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only ISalesOrderRepository wrapper that replaces GetNextOrderNumberAsync
/// (which calls PostgreSQL nextval()) with an in-process atomic counter.
/// All other calls delegate to the real SalesOrderRepository.
/// </summary>
public sealed class TestSalesOrderRepository(ISalesOrderRepository inner) : ISalesOrderRepository
{
    private static long _counter = 0;

    public Task<long> GetNextOrderNumberAsync(CancellationToken ct = default)
        => Task.FromResult(Interlocked.Increment(ref _counter));

    public Task<SalesOrder?> GetByIdAsync(int id, CancellationToken ct = default)
        => inner.GetByIdAsync(id, ct);

    public Task<SalesOrder?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => inner.GetByIdWithItemsAsync(id, ct);

    public Task<(IEnumerable<SalesOrder> SalesOrders, int TotalCount)> GetAllAsync(
        int skip, int take, string? search = null,
        SalesOrderStatus? status = null, int? distributorId = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken ct = default)
        => inner.GetAllAsync(skip, take, search, status, distributorId, fromDate, toDate, ct);

    public Task CreateAsync(SalesOrder order, CancellationToken ct = default)
        => inner.CreateAsync(order, ct);

    public Task UpdateAsync(SalesOrder order, CancellationToken ct = default)
        => inner.UpdateAsync(order, ct);

    public Task AddItemsAsync(IEnumerable<SalesOrderItem> items, CancellationToken ct = default)
        => inner.AddItemsAsync(items, ct);

    public Task RemoveItemsAsync(int salesOrderId, CancellationToken ct = default)
        => inner.RemoveItemsAsync(salesOrderId, ct);

    public Task AddHistoryAsync(SalesOrderHistory history, CancellationToken ct = default)
        => inner.AddHistoryAsync(history, ct);

    public Task<IEnumerable<SalesOrderHistory>> GetHistoryAsync(int salesOrderId, CancellationToken ct = default)
        => inner.GetHistoryAsync(salesOrderId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
