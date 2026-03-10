using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Territories.Repositories;

public class TerritoryRepository(AppDbContext context) : ITerritoryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Territory?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Territories
            .Include(t => t.Area)
                .ThenInclude(a => a!.Region)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IEnumerable<Territory> Territories, int TotalCount)> GetAllAsync(int skip, int take, int? areaId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var query = _context.Territories.AsQueryable();
        if (areaId.HasValue) query = query.Where(t => t.AreaId == areaId.Value);
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync(ct);
        var territories = await query
            .Include(t => t.Area)
                .ThenInclude(a => a!.Region)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (territories, totalCount);
    }

    public async Task<IEnumerable<Territory>> GetAllActiveAsync(int? areaId = null, CancellationToken ct = default)
    {
        var query = _context.Territories.Where(t => t.IsActive);
        if (areaId.HasValue) query = query.Where(t => t.AreaId == areaId.Value);
        return await query
            .Include(t => t.Area)
                .ThenInclude(a => a!.Region)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<Area?> GetAreaWithRegionAsync(int areaId, CancellationToken ct = default)
        => await _context.Areas
            .Include(a => a.Region)
            .FirstOrDefaultAsync(a => a.Id == areaId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int areaId, CancellationToken ct = default)
        => await _context.Territories.AnyAsync(t => t.Name == name && t.AreaId == areaId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int areaId, int excludeId, CancellationToken ct = default)
        => await _context.Territories.AnyAsync(t => t.Name == name && t.AreaId == areaId && t.Id != excludeId, ct);

    public async Task<bool> AreaExistsAsync(int areaId, CancellationToken ct = default)
        => await _context.Areas.AnyAsync(a => a.Id == areaId, ct);

    public async Task CreateAsync(Territory territory, CancellationToken ct = default)
        => await _context.Territories.AddAsync(territory, ct);

    public Task UpdateAsync(Territory territory, CancellationToken ct = default)
    {
        _context.Territories.Update(territory);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
