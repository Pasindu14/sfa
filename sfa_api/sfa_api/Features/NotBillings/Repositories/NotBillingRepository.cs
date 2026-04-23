using Microsoft.EntityFrameworkCore;
using sfa_api.Features.NotBillings.DTOs;
using sfa_api.Features.NotBillings.Entities;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.NotBillings.Repositories;

public class NotBillingRepository(AppDbContext db) : INotBillingRepository
{
    private readonly AppDbContext _db = db;

    public async Task<long> GetNextNotBillingNumberAsync(CancellationToken ct = default)
    {
        var result = await _db.Database
            .SqlQueryRaw<long>("SELECT nextval('not_billing_number_seq')")
            .ToListAsync(ct);
        return result[0];
    }

    public Task<Outlet?> GetOutletAsync(int outletId, CancellationToken ct = default)
        => _db.Outlets
              .AsNoTracking()
              .FirstOrDefaultAsync(x => x.Id == outletId && x.IsActive && !x.IsDeleted, ct);

    public Task<bool> ExistsForOutletTodayAsync(int salesRepId, int outletId, DateOnly date, CancellationToken ct = default)
        => _db.NotBillings
              .AnyAsync(x => x.SalesRepId == salesRepId
                          && x.OutletId == outletId
                          && x.NotBillingDate == date
                          && !x.IsDeleted, ct);

    public Task<NotBilling?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.NotBillings
              .AsNoTracking()
              .Include(x => x.Outlet)
              .Include(x => x.SalesRep)
              .Include(x => x.Supervisor)
              .Include(x => x.Asm)
              .Include(x => x.Rsm)
              .Include(x => x.Nsm)
              .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<(List<NotBillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        int? outletId, int? salesRepId,
        NotBillingReason? reason,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.NotBillings
            .AsNoTracking()
            .Include(x => x.Outlet)
            .Include(x => x.SalesRep)
            .Where(x => !x.IsDeleted);

        if (outletId.HasValue)
            query = query.Where(x => x.OutletId == outletId.Value);

        if (salesRepId.HasValue)
            query = query.Where(x => x.SalesRepId == salesRepId.Value);

        if (reason.HasValue)
            query = query.Where(x => x.Reason == reason.Value);

        if (dateFrom.HasValue)
            query = query.Where(x => x.NotBillingDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.NotBillingDate <= dateTo.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.NotBillingDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotBillingListDto(
                x.Id,
                x.NotBillingNumber,
                x.NotBillingDate,
                x.OutletId,
                x.Outlet.Name,
                x.SalesRepId,
                x.SalesRep.Name,
                x.Reason,
                x.CreatedAt))
            .ToListAsync(ct);

        return (items, total);
    }

    public Task AddAsync(NotBilling notBilling, CancellationToken ct = default)
    {
        _db.NotBillings.Add(notBilling);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
