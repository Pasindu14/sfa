using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Territories.Repositories;

public class TerritoryRepository(AppDbContext context) : ITerritoryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Territory?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Territories
            .IgnoreQueryFilters()
            .Include(t => t.Area)
                .ThenInclude(a => a!.Region)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IEnumerable<Territory> Territories, int TotalCount)> GetAllAsync(int skip, int take, int? areaId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Territories.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();
        if (areaId.HasValue) query = query.Where(t => t.AreaId == areaId.Value);
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(t => EF.Functions.ILike(t.Name, pattern))
                : query.Where(t => EF.Functions.Like(t.Name, pattern));
        }

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
            .IgnoreQueryFilters()
            .Include(a => a.Region)
            .FirstOrDefaultAsync(a => a.Id == areaId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int areaId, CancellationToken ct = default)
    {
        var query = _context.Territories.IgnoreQueryFilters().Where(t => t.AreaId == areaId);
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(t => EF.Functions.ILike(t.Name, name))   // case-insensitive duplicate check
            : query.Where(t => EF.Functions.Like(t.Name, name));
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int areaId, int excludeId, CancellationToken ct = default)
    {
        var query = _context.Territories.IgnoreQueryFilters().Where(t => t.AreaId == areaId && t.Id != excludeId);
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(t => EF.Functions.ILike(t.Name, name))
            : query.Where(t => EF.Functions.Like(t.Name, name));
        return await query.AnyAsync(ct);
    }

    public async Task<bool> AreaExistsAsync(int areaId, CancellationToken ct = default)
        => await _context.Areas.IgnoreQueryFilters().AnyAsync(a => a.Id == areaId, ct);

    public async Task CreateAsync(Territory territory, CancellationToken ct = default)
        => await _context.Territories.AddAsync(territory, ct);

    public Task UpdateAsync(Territory territory, CancellationToken ct = default)
    {
        _context.Territories.Update(territory);
        return Task.CompletedTask;
    }

    // Sets the OriginalValue of RowVersion so EF uses the client's version in the
    // WHERE xmin = $token clause — this is what detects cross-request staleness.
    public void ApplyConcurrencyToken(Territory territory, uint rowVersion)
        => _context.Entry(territory).Property(x => x.RowVersion).OriginalValue = rowVersion;

    public async Task<bool> HasActiveDivisionsAsync(int territoryId, CancellationToken ct = default)
        => await _context.Divisions
            .IgnoreQueryFilters()
            .AnyAsync(d => d.TerritoryId == territoryId && d.IsActive && !d.IsDeleted, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }
}
