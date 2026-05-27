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
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(IEnumerable<Outlet> Outlets, int TotalCount)> GetAllAsync(
        int skip, int take, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Outlets.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();

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

    public async Task<(IEnumerable<Outlet> Outlets, int TotalCount)> GetAllByTerritoryAsync(
        int territoryId, int skip, int take,
        bool? isActive = null, string? search = null,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Outlets
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && x.TerritoryId == territoryId)
            .AsQueryable();

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

    public async Task<IEnumerable<Outlet>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Outlets
            .Where(o => o.IsActive)
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
            .ToListAsync(ct);

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

    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _context.Outlets
            .IgnoreQueryFilters()
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.IsActive, false)
                .SetProperty(o => o.IsDeleted, true)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
