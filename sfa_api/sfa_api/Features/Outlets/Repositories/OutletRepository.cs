using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Outlets.DTOs;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Infrastructure.Persistence;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Outlets.Repositories;

public class OutletRepository(AppDbContext context) : IOutletRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Outlet?> GetByIdAsync(int id, CancellationToken ct = default)
        // IgnoreQueryFilters so a deactivated outlet can still be fetched (e.g. to reactivate),
        // but a soft-DELETED outlet is never returned.
        => await _context.Outlets
            .IgnoreQueryFilters()
            .Include(o => o.Route)
                .ThenInclude(r => r!.Division)
            .Include(o => o.Route)
                .ThenInclude(r => r!.Territory)
            .Include(o => o.Route)
                .ThenInclude(r => r!.Area)
            .Include(o => o.Route)
                .ThenInclude(r => r!.Region)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct);

    public async Task<(IEnumerable<Outlet> Outlets, int TotalCount)> GetAllAsync(
        int skip, int take, bool? isActive = null, string? search = null,
        int? territoryId = null, int? routeId = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Outlets.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();

        if (territoryId.HasValue) query = query.Where(o => o.TerritoryId == territoryId.Value);
        if (routeId.HasValue) query = query.Where(o => o.RouteId == routeId.Value);
        if (isActive.HasValue) query = query.Where(o => o.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(o => EF.Functions.ILike(o.Name, pattern))
                : query.Where(o => EF.Functions.Like(o.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(o => o.Route)
                .ThenInclude(r => r!.Division)
            .Include(o => o.Route)
                .ThenInclude(r => r!.Territory)
            .Include(o => o.Route)
                .ThenInclude(r => r!.Area)
            .Include(o => o.Route)
                .ThenInclude(r => r!.Region)
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<List<OutletDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        // Column projection instead of materialising Outlet + Route + 4 ancestor entities.
        // Explicit INNER JOINs reproduce the old Include semantics exactly: Outlet.RouteId and the
        // Route's ancestor FKs are required, so each Include was an INNER JOIN against the global
        // query filter (IsActive && !IsDeleted) — an outlet whose route or any route ancestor is
        // inactive/deleted was, and still is, left out.
        var rows = await (
                from o in _context.Outlets.Where(o => o.IsActive)
                join r in _context.Routes      on o.RouteId     equals r.Id
                join d in _context.Divisions   on r.DivisionId  equals d.Id
                join t in _context.Territories on r.TerritoryId equals t.Id
                join a in _context.Areas       on r.AreaId      equals a.Id
                join g in _context.Regions     on r.RegionId    equals g.Id
                orderby o.Name, o.Id   // Id tiebreak only fixes the (previously undefined) order among equal names
                select new
                {
                    o.Id, o.Name, o.Address, o.Tel, o.Email, o.ContactPerson, o.NicNo, o.VatNo,
                    o.CreditLimit, o.Latitude, o.Longitude, o.OwnerDOB, o.Remarks, o.Image,
                    o.OutletType, o.OutletCategory, o.ProvinceCode, o.DistrictCode,
                    o.RouteId, RouteName = r.Name,
                    o.DivisionId, DivisionName = d.Name,
                    o.TerritoryId, TerritoryName = t.Name,
                    o.AreaId, AreaName = a.Name,
                    o.RegionId, RegionName = g.Name,
                    o.IsActive, o.RowVersion, o.CreatedAt, o.UpdatedAt, o.LastBillDate,
                })
            .AsNoTracking()
            .ToListAsync(ct);

        // Enum → string in memory, matching OutletService.MapToDto (Enum.ToString()).
        return rows.Select(o => new OutletDto(
            Id: o.Id,
            Name: o.Name,
            Address: o.Address,
            Tel: o.Tel,
            Email: o.Email,
            ContactPerson: o.ContactPerson,
            NicNo: o.NicNo,
            VatNo: o.VatNo,
            CreditLimit: o.CreditLimit,
            Latitude: o.Latitude,
            Longitude: o.Longitude,
            OwnerDOB: o.OwnerDOB,
            Remarks: o.Remarks,
            Image: o.Image,
            OutletType: o.OutletType.ToString(),
            OutletCategory: o.OutletCategory.ToString(),
            ProvinceCode: o.ProvinceCode,
            DistrictCode: o.DistrictCode,
            RouteId: o.RouteId,
            RouteName: o.RouteName ?? string.Empty,
            DivisionId: o.DivisionId,
            DivisionName: o.DivisionName ?? string.Empty,
            TerritoryId: o.TerritoryId,
            TerritoryName: o.TerritoryName ?? string.Empty,
            AreaId: o.AreaId,
            AreaName: o.AreaName ?? string.Empty,
            RegionId: o.RegionId,
            RegionName: o.RegionName ?? string.Empty,
            IsActive: o.IsActive,
            RowVersion: o.RowVersion,
            CreatedAt: o.CreatedAt,
            UpdatedAt: o.UpdatedAt,
            LastBillDate: o.LastBillDate)).ToList();
    }

    public async Task<RouteEntity?> GetRouteWithAncestorsAsync(int routeId, CancellationToken ct = default)
        => await _context.Routes
            .IgnoreQueryFilters()
            .Include(r => r.Division)
            .Include(r => r.Territory)
            .Include(r => r.Area)
            .Include(r => r.Region)
            .FirstOrDefaultAsync(r => r.Id == routeId, ct);

    public async Task<bool> ExistsByNicNoAsync(string nicNo, CancellationToken ct = default)
        => await _context.Outlets.IgnoreQueryFilters().AnyAsync(o => o.NicNo == nicNo, ct);

    public async Task<bool> ExistsByNicNoAsync(string nicNo, int excludeId, CancellationToken ct = default)
        => await _context.Outlets.IgnoreQueryFilters().AnyAsync(o => o.NicNo == nicNo && o.Id != excludeId, ct);

    public async Task<IEnumerable<Outlet>> GetByRouteIdAsync(int routeId, CancellationToken ct = default)
        => await _context.Outlets
            .Where(o => o.RouteId == routeId && o.IsActive && !o.IsDeleted)
            .Include(o => o.Route)   // only needed for RouteName — ancestor IDs already denormalized on Outlet
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .ToListAsync(ct);

    public async Task<IEnumerable<OutletMapPointDto>> GetMapPointsAsync(CancellationToken ct = default)
        => await _context.Outlets
            .Where(o => o.IsActive)
            .Select(o => new OutletMapPointDto(o.Id, o.Name, o.Latitude, o.Longitude))
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task CreateAsync(Outlet outlet, CancellationToken ct = default)
        => await _context.Outlets.AddAsync(outlet, ct);

    public Task UpdateAsync(Outlet outlet, CancellationToken ct = default)
    {
        _context.Outlets.Update(outlet);
        return Task.CompletedTask;
    }

    // Sets the OriginalValue of RowVersion so EF uses the client's version in the
    // WHERE xmin = $token clause — this is what detects cross-request staleness.
    public void ApplyConcurrencyToken(Outlet outlet, uint rowVersion)
        => _context.Entry(outlet).Property(x => x.RowVersion).OriginalValue = rowVersion;

    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _context.Outlets
            .IgnoreQueryFilters()
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.IsActive, false)
                .SetProperty(o => o.IsDeleted, true)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new sfa_api.Common.Errors.ConcurrencyConflictException();
        }
    }
}
