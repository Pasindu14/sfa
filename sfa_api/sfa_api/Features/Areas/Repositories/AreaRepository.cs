using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Areas.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Areas.Repositories;

public class AreaRepository(AppDbContext context) : IAreaRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Area?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Areas
            .IgnoreQueryFilters()
            .Include(a => a.Region)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IEnumerable<Area> Areas, int TotalCount)> GetAllAsync(int skip, int take, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Areas.IgnoreQueryFilters().AsQueryable();
        if (regionId.HasValue) query = query.Where(a => a.RegionId == regionId.Value);
        if (isActive.HasValue) query = query.Where(a => a.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => EF.Functions.ILike(a.Name, $"%{search}%"));

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

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
