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

    public Task<List<DistributorStock>> GetStockSnapshotAsync(
        int distributorId, IEnumerable<int> productIds, CancellationToken ct = default)
        => _db.DistributorStocks
              .AsNoTracking()
              .Where(x => x.DistributorId == distributorId && productIds.Contains(x.ProductId))
              .ToListAsync(ct);

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
        BillingType? billingType, BillingStatus? status,
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

        if (billingType.HasValue)
            query = query.Where(x => x.BillingType == billingType.Value);

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
                x.BillingType,
                x.ReturnType,
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
