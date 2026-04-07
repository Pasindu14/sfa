using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Areas.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Areas.Repositories;

public class AreaRepository(AppDbContext context) : IAreaRepository
{
    private readonly AppDbContext _context = context;

    // Read-only fetch — no tracking, safe for projections and GetById responses
    public async Task<Area?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Areas
            .IgnoreQueryFilters()
            .Include(a => a.Region)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    // Tracked fetch — used before mutations (Update, Activate, Deactivate)
    public async Task<Area?> GetByIdTrackedAsync(int id, CancellationToken ct = default)
        => await _context.Areas
            .IgnoreQueryFilters()
            .Include(a => a.Region)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IEnumerable<Area> Areas, int TotalCount)> GetAllAsync(int skip, int take, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Areas.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();
        if (regionId.HasValue) query = query.Where(a => a.RegionId == regionId.Value);
        if (isActive.HasValue) query = query.Where(a => a.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(a => EF.Functions.ILike(a.Name, pattern))
                : query.Where(a => EF.Functions.Like(a.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var areas = await query
            .Include(a => a.Region)
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (areas, totalCount);
    }

    public async Task<IEnumerable<Area>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default)
    {
        var query = _context.Areas.Where(a => a.IsActive);
        if (regionId.HasValue) query = query.Where(a => a.RegionId == regionId.Value);
        return await query
            .Include(a => a.Region)
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Take(1000)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int regionId, CancellationToken ct = default)
        => await _context.Areas.IgnoreQueryFilters().AnyAsync(a => a.Name == name && a.RegionId == regionId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int regionId, int excludeId, CancellationToken ct = default)
        => await _context.Areas.IgnoreQueryFilters().AnyAsync(a => a.Name == name && a.RegionId == regionId && a.Id != excludeId, ct);

    public async Task<bool> RegionExistsAsync(int regionId, CancellationToken ct = default)
        => await _context.Regions.IgnoreQueryFilters().AnyAsync(r => r.Id == regionId, ct);

    public async Task CreateAsync(Area area, CancellationToken ct = default)
        => await _context.Areas.AddAsync(area, ct);

    public Task UpdateAsync(Area area, CancellationToken ct = default)
    {
        _context.Areas.Update(area);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var area = await _context.Areas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (area is null) return;

        area.IsActive = false;
        area.IsDeleted = true;
        area.UpdatedAt = DateTime.UtcNow;
        _context.Areas.Update(area);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
