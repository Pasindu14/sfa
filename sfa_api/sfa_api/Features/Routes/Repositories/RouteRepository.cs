using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Infrastructure.Persistence;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Routes.Repositories;

public class RouteRepository(AppDbContext context) : IRouteRepository
{
    private readonly AppDbContext _context = context;

    public async Task<RouteEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Routes
            .Include(r => r.Division)
                .ThenInclude(d => d!.Territory)
                    .ThenInclude(t => t!.Area)
                        .ThenInclude(a => a!.Region)
            .Include(r => r.Territory)
            .Include(r => r.Area)
            .Include(r => r.Region)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(IEnumerable<RouteEntity> Routes, int TotalCount)> GetAllAsync(
        int skip, int take, string? search = null, CancellationToken ct = default)
    {
        var query = _context.Routes
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(r => r.Division)
                .ThenInclude(d => d!.Territory)
                    .ThenInclude(t => t!.Area)
                        .ThenInclude(a => a!.Region)
            .Include(r => r.Territory)
            .Include(r => r.Area)
            .Include(r => r.Region)
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Division?> GetDivisionWithAncestorsAsync(int divisionId, CancellationToken ct = default)
        => await _context.Divisions
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .FirstOrDefaultAsync(d => d.Id == divisionId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int divisionId, CancellationToken ct = default)
        => await _context.Routes.AnyAsync(r => r.Name == name && r.DivisionId == divisionId && !r.IsDeleted, ct);

    public async Task<bool> ExistsByNameAsync(string name, int divisionId, int excludeId, CancellationToken ct = default)
        => await _context.Routes.AnyAsync(r => r.Name == name && r.DivisionId == divisionId && r.Id != excludeId && !r.IsDeleted, ct);

    public async Task CreateAsync(RouteEntity route, CancellationToken ct = default)
        => await _context.Routes.AddAsync(route, ct);

    public Task UpdateAsync(RouteEntity route, CancellationToken ct = default)
    {
        _context.Routes.Update(route);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var route = await _context.Routes.FindAsync([id], ct);
        if (route != null)
        {
            route.IsDeleted = true;
            _context.Routes.Update(route);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
