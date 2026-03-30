using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Regions.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Regions.Repositories;

public class RegionRepository(AppDbContext context) : IRegionRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Region?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Regions.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(IEnumerable<Region> Regions, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Regions.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(r => EF.Functions.ILike(r.Name, pattern))
                : query.Where(r => EF.Functions.Like(r.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var regions = await query
            .AsNoTracking()
            .OrderBy(r => r.Name)
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
        => await _context.Regions.IgnoreQueryFilters().AnyAsync(r => r.Name == name, ct);

    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default)
        => await _context.Regions.IgnoreQueryFilters().AnyAsync(r => r.Name == name && r.Id != excludeId, ct);

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
