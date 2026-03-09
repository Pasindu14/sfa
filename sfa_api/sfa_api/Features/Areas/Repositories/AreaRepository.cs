using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Areas.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Areas.Repositories;

public class AreaRepository(AppDbContext context) : IAreaRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Area?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Areas
            .Include(a => a.Region)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IEnumerable<Area> Areas, int TotalCount)> GetAllAsync(int skip, int take, CancellationToken ct = default)
    {
        var totalCount = await _context.Areas.CountAsync(ct);
        var areas = await _context.Areas
            .AsNoTracking()
            .Include(a => a.Region)
            .OrderBy(a => a.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (areas, totalCount);
    }

    public async Task<IEnumerable<Area>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Areas
            .AsNoTracking()
            .Include(a => a.Region)
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, int regionId, CancellationToken ct = default)
        => await _context.Areas.AnyAsync(a => a.Name == name && a.RegionId == regionId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int regionId, int excludeId, CancellationToken ct = default)
        => await _context.Areas.AnyAsync(a => a.Name == name && a.RegionId == regionId && a.Id != excludeId, ct);

    public async Task<bool> RegionExistsAsync(int regionId, CancellationToken ct = default)
        => await _context.Regions.AnyAsync(r => r.Id == regionId, ct);

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
