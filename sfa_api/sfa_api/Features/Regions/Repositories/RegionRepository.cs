using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
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
            // Search by region name or exact "code" (numeric Id). Name substring rides the
            // pg_trgm GIN index; the code branch is an exact PK lookup (parse to int).
            var pattern = $"%{search}%";
            var isCode = int.TryParse(search.Trim(), out var codeId);
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(r => EF.Functions.ILike(r.Name, pattern) || (isCode && r.Id == codeId))
                : query.Where(r => EF.Functions.Like(r.Name, pattern) || (isCode && r.Id == codeId));
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
    {
        var query = _context.Regions.IgnoreQueryFilters();
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(r => EF.Functions.ILike(r.Name, name))   // case-insensitive duplicate check
            : query.Where(r => EF.Functions.Like(r.Name, name));
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default)
    {
        var query = _context.Regions.IgnoreQueryFilters().Where(r => r.Id != excludeId);
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(r => EF.Functions.ILike(r.Name, name))
            : query.Where(r => EF.Functions.Like(r.Name, name));
        return await query.AnyAsync(ct);
    }

    public async Task CreateAsync(Region region, CancellationToken ct = default)
        => await _context.Regions.AddAsync(region, ct);

    public Task UpdateAsync(Region region, CancellationToken ct = default)
    {
        _context.Regions.Update(region);
        return Task.CompletedTask;
    }

    // Sets the OriginalValue of RowVersion so EF uses the client's version in the
    // WHERE xmin = $token clause — this is what detects cross-request staleness.
    public void ApplyConcurrencyToken(Region region, uint rowVersion)
        => _context.Entry(region).Property(x => x.RowVersion).OriginalValue = rowVersion;

    public async Task<bool> HasActiveAreasAsync(int regionId, CancellationToken ct = default)
        => await _context.Areas
            .IgnoreQueryFilters()
            .AnyAsync(a => a.RegionId == regionId && a.IsActive && !a.IsDeleted, ct);

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
