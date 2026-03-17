using Microsoft.EntityFrameworkCore;
using sfa_api.Features.SalesOrders.Entities;
using sfa_api.Features.SalesOrders.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.SalesOrders.Repositories;

public class SalesOrderRepository(AppDbContext context) : ISalesOrderRepository
{
    private readonly AppDbContext _context = context;

    public async Task<SalesOrder?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.SalesOrders
            .Include(o => o.Distributor)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<SalesOrder?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => await _context.SalesOrders
            .Include(o => o.Distributor)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(IEnumerable<SalesOrder> SalesOrders, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        SalesOrderStatus? status = null,
        int? distributorId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = _context.SalesOrders
            .Include(o => o.Distributor)
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.OrderNumber.ToLower().Contains(search.ToLower())
                || o.Distributor.Name.ToLower().Contains(search.ToLower()));

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (distributorId.HasValue)
            query = query.Where(o => o.DistributorId == distributorId.Value);

        if (fromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= from);
        }
        if (toDate.HasValue)
        {
            var to = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt < to);
        }

        var totalCount = await query.CountAsync(ct);
        var salesOrders = await query
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (salesOrders, totalCount);
    }

    public async Task<long> GetNextOrderNumberAsync(CancellationToken ct = default)
    {
        var result = await _context.Database
            .SqlQueryRaw<long>("SELECT nextval('sales_order_number_seq')")
            .FirstAsync(ct);
        return result;
    }

    public async Task CreateAsync(SalesOrder order, CancellationToken ct = default)
        => await _context.SalesOrders.AddAsync(order, ct);

    public Task UpdateAsync(SalesOrder order, CancellationToken ct = default)
    {
        _context.SalesOrders.Update(order);
        return Task.CompletedTask;
    }

    public async Task AddItemsAsync(IEnumerable<SalesOrderItem> items, CancellationToken ct = default)
        => await _context.SalesOrderItems.AddRangeAsync(items, ct);

    public Task RemoveItemsAsync(int salesOrderId, CancellationToken ct = default)
    {
        var items = _context.SalesOrderItems.Where(i => i.SalesOrderId == salesOrderId);
        _context.SalesOrderItems.RemoveRange(items);
        return Task.CompletedTask;
    }

    public async Task AddHistoryAsync(SalesOrderHistory history, CancellationToken ct = default)
        => await _context.SalesOrderHistories.AddAsync(history, ct);

    public async Task<IEnumerable<SalesOrderHistory>> GetHistoryAsync(int salesOrderId, CancellationToken ct = default)
    {
        return await _context.SalesOrderHistories
            .Where(h => h.SalesOrderId == salesOrderId)
            .OrderBy(h => h.PerformedAt)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
