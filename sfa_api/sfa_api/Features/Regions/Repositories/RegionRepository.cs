using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Regions.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Regions.Repositories;

public class RegionRepository(AppDbContext context) : IRegionRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Region?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Regions.FindAsync([id], ct);

    public async Task<(IEnumerable<Region> Regions, int TotalCount)> GetAllAsync(int skip, int take, CancellationToken ct = default)
    {
        var totalCount = await _context.Regions.CountAsync(ct);
        var regions = await _context.Regions
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (regions, totalCount);
    }

    public async Task<IEnumerable<Region>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Regions
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await _context.Regions.AnyAsync(r => r.Name == name, ct);

    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default)
        => await _context.Regions.AnyAsync(r => r.Name == name && r.Id != excludeId, ct);

    public async Task CreateAsync(Region region, CancellationToken ct = default)
        => await _context.Regions.AddAsync(region, ct);

    public Task UpdateAsync(Region region, CancellationToken ct = default)
    {
        _context.Regions.Update(region);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
