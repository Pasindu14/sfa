using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Divisions.Repositories;

public class DivisionRepository(AppDbContext context) : IDivisionRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Division?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Divisions
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IEnumerable<Division> Divisions, int TotalCount)> GetAllAsync(
        int skip, int take,
        int? territoryId = null, int? areaId = null, int? regionId = null,
        bool? isActive = null, string? search = null,
        CancellationToken ct = default)
    {
        var query = _context.Divisions.AsQueryable();
        if (territoryId.HasValue) query = query.Where(d => d.TerritoryId == territoryId.Value);
        if (areaId.HasValue) query = query.Where(d => d.AreaId == areaId.Value);
        if (regionId.HasValue) query = query.Where(d => d.RegionId == regionId.Value);
        if (isActive.HasValue) query = query.Where(d => d.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync(ct);
        var divisions = await query
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (divisions, totalCount);
    }

    public async Task<IEnumerable<Division>> GetAllActiveAsync(int? territoryId = null, CancellationToken ct = default)
    {
        var query = _context.Divisions.Where(d => d.IsActive);
        if (territoryId.HasValue) query = query.Where(d => d.TerritoryId == territoryId.Value);
        return await query
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
    }

    public async Task<Territory?> GetTerritoryWithAncestorsAsync(int territoryId, CancellationToken ct = default)
        => await _context.Territories
            .Include(t => t.Area)
                .ThenInclude(a => a!.Region)
            .FirstOrDefaultAsync(t => t.Id == territoryId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int territoryId, CancellationToken ct = default)
        => await _context.Divisions.AnyAsync(d => d.Name == name && d.TerritoryId == territoryId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int territoryId, int excludeId, CancellationToken ct = default)
        => await _context.Divisions.AnyAsync(d => d.Name == name && d.TerritoryId == territoryId && d.Id != excludeId, ct);

    public async Task<bool> TerritoryExistsAsync(int territoryId, CancellationToken ct = default)
        => await _context.Territories.AnyAsync(t => t.Id == territoryId, ct);

    public async Task CreateAsync(Division division, CancellationToken ct = default)
        => await _context.Divisions.AddAsync(division, ct);

    public Task UpdateAsync(Division division, CancellationToken ct = default)
    {
        _context.Divisions.Update(division);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
