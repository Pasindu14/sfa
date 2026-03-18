using Microsoft.EntityFrameworkCore;
using sfa_api.Features.PurchaseOrders.Entities;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.PurchaseOrders.Repositories;

public class PurchaseOrderRepository(AppDbContext context) : IPurchaseOrderRepository
{
    private readonly AppDbContext _context = context;

    public async Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.PurchaseOrders
            .Include(o => o.Distributor)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<PurchaseOrder?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => await _context.PurchaseOrders
            .Include(o => o.Distributor)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(IEnumerable<PurchaseOrder> PurchaseOrders, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        PurchaseOrderStatus? status = null,
        int? distributorId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = _context.PurchaseOrders
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
        var purchaseOrders = await query
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (purchaseOrders, totalCount);
    }

    public async Task<long> GetNextOrderNumberAsync(CancellationToken ct = default)
    {
        var result = await _context.Database
            .SqlQueryRaw<long>("SELECT nextval('purchase_order_number_seq') AS \"Value\"")
            .FirstAsync(ct);
        return result;
    }

    public async Task<Dictionary<PurchaseOrderStatus, int>> GetCountsByStatusAsync(
        int? distributorId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = _context.PurchaseOrders.AsQueryable();

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

        return await query
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    }

    public async Task CreateAsync(PurchaseOrder order, CancellationToken ct = default)
        => await _context.PurchaseOrders.AddAsync(order, ct);

    public Task UpdateAsync(PurchaseOrder order, CancellationToken ct = default)
    {
        _context.PurchaseOrders.Update(order);
        return Task.CompletedTask;
    }

    public async Task AddItemsAsync(IEnumerable<PurchaseOrderItem> items, CancellationToken ct = default)
        => await _context.PurchaseOrderItems.AddRangeAsync(items, ct);

    public Task RemoveItemsAsync(int purchaseOrderId, CancellationToken ct = default)
    {
        var items = _context.PurchaseOrderItems.Where(i => i.PurchaseOrderId == purchaseOrderId);
        _context.PurchaseOrderItems.RemoveRange(items);
        return Task.CompletedTask;
    }

    public async Task AddHistoryAsync(PurchaseOrderHistory history, CancellationToken ct = default)
        => await _context.PurchaseOrderHistories.AddAsync(history, ct);

    public async Task<IEnumerable<PurchaseOrderHistory>> GetHistoryAsync(int purchaseOrderId, CancellationToken ct = default)
    {
        return await _context.PurchaseOrderHistories
            .Where(h => h.PurchaseOrderId == purchaseOrderId)
            .OrderBy(h => h.PerformedAt)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
