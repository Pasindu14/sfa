using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Billings.Repositories;

public class BillingRepository(AppDbContext db) : IBillingRepository
{
    private readonly AppDbContext _db = db;

    public async Task<long> GetNextBillingNumberAsync(CancellationToken ct = default)
    {
        var result = await _db.Database
            .SqlQueryRaw<long>("SELECT nextval('billing_number_seq')")
            .ToListAsync(ct);
        return result[0];
    }

    public Task<Outlet?> GetOutletAsync(int outletId, CancellationToken ct = default)
        => _db.Outlets
              .AsNoTracking()
              .FirstOrDefaultAsync(x => x.Id == outletId && x.IsActive && !x.IsDeleted, ct);

    public Task<Distributor?> GetDistributorByTerritoryAsync(int territoryId, CancellationToken ct = default)
        => _db.Distributors
              .AsNoTracking()
              .FirstOrDefaultAsync(x => x.TerritoryId == territoryId && x.IsActive && !x.IsDeleted, ct);

    public async Task<List<int>> GetActiveProductIdsAsync(IEnumerable<int> productIds, CancellationToken ct = default)
        => await _db.Products
                    .Where(x => productIds.Contains(x.Id) && x.IsActive && !x.IsDeleted)
                    .Select(x => x.Id)
                    .ToListAsync(ct);

    public async Task<Dictionary<int, string>> GetActiveProductNamesAsync(IEnumerable<int> productIds, CancellationToken ct = default)
        => await _db.Products
                    .AsNoTracking()
                    .Where(x => productIds.Contains(x.Id) && x.IsActive && !x.IsDeleted)
                    .Select(x => new { x.Id, x.ItemDescription })
                    .ToDictionaryAsync(x => x.Id, x => x.ItemDescription, ct);

    public Task<List<DistributorStock>> GetStockSnapshotAsync(
        int distributorId, IEnumerable<int> productIds, CancellationToken ct = default)
        => _db.DistributorStocks
              .AsNoTracking()
              .Where(x => x.DistributorId == distributorId && productIds.Contains(x.ProductId))
              .ToListAsync(ct);

    public Task<Billing?> GetTrackedByIdAsync(int id, CancellationToken ct = default)
        => _db.Billings
              .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<Billing?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Billings
              .AsNoTracking()
              .Include(x => x.Outlet)
              .Include(x => x.SalesRep)
              .Include(x => x.Distributor)
              .Include(x => x.Supervisor)
              .Include(x => x.Asm)
              .Include(x => x.Rsm)
              .Include(x => x.Nsm)
              .Include(x => x.Items)
                  .ThenInclude(i => i.Product)
              .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<(List<BillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        BillingStatus? status,
        int? outletId, int? distributorId, int? salesRepId,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Billings
            .AsNoTracking()
            .Include(x => x.Outlet)
            .Include(x => x.SalesRep)
            .Include(x => x.Distributor)
            .Where(x => !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (outletId.HasValue)
            query = query.Where(x => x.OutletId == outletId.Value);

        if (distributorId.HasValue)
            query = query.Where(x => x.DistributorId == distributorId.Value);

        if (salesRepId.HasValue)
            query = query.Where(x => x.SalesRepId == salesRepId.Value);

        if (dateFrom.HasValue)
            query = query.Where(x => x.BillingDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.BillingDate <= dateTo.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.BillingDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BillingListDto(
                x.Id,
                x.BillingNumber,
                x.BillingDate,
                x.OutletId,
                x.Outlet.Name,
                x.SalesRepId,
                x.SalesRep.Name,
                x.DistributorId,
                x.Distributor.Name,
                x.TotalAmount,
                x.Status,
                x.CreatedAt))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<List<OutletBillingSummaryRawRow>> GetOutletSummaryRawAsync(
        int salesRepId, int routeId,
        DateOnly dateFrom, DateOnly dateTo,
        CancellationToken ct = default)
        => await _db.Billings
            .AsNoTracking()
            .Where(b => b.SalesRepId == salesRepId
                     && b.RouteId == routeId
                     && b.BillingDate >= dateFrom
                     && b.BillingDate <= dateTo)
            .Select(b => new OutletBillingSummaryRawRow(
                b.OutletId,
                b.Outlet.Name,
                b.Id,
                b.BillingNumber,
                b.BillingDate,
                b.TotalAmount,
                b.Status))
            .ToListAsync(ct);

    public async Task<decimal> GetRepMonthlySalesTotalAsync(
        int salesRepId, int year, int month, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to   = from.AddMonths(1);
        return await _db.Billings
            .AsNoTracking()
            .Where(b => b.SalesRepId   == salesRepId
                     && b.BillingDate  >= from
                     && b.BillingDate  <  to
                     && b.IsActive
                     && !b.IsDeleted)
            .SumAsync(b => b.TotalAmount, ct);
    }

    public async Task<List<RepProductSalesRow>> GetRepMonthlySalesByProductAsync(
        int salesRepId, int year, int month, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to   = from.AddMonths(1);
        return await _db.BillingItems
            .AsNoTracking()
            .Where(bi => bi.BillingItemType == BillingItemType.Sale
                      && bi.Billing.SalesRepId == salesRepId
                      && bi.Billing.BillingDate >= from
                      && bi.Billing.BillingDate <  to
                      && bi.Billing.IsActive
                      && !bi.Billing.IsDeleted)
            .GroupBy(bi => bi.ProductId)
            .Select(g => new RepProductSalesRow(
                g.Key,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.TotalPrice)))
            .ToListAsync(ct);
    }

    public Task AddAsync(Billing billing, CancellationToken ct = default)
    {
        _db.Billings.Add(billing);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _db.Database.BeginTransactionAsync(ct);
}
