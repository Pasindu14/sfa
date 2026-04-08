using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.DTOs;
using sfa_api.Features.Areas.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Areas.Repositories;

public class AreaRepository(AppDbContext context) : IAreaRepository
{
    private readonly AppDbContext _context = context;

    // Read-only fetch — IgnoreQueryFilters to see inactive records; IsDeleted guard applied manually
    public async Task<Area?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Areas
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted)
            .Include(a => a.Region)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    // Tracked fetch — used before mutations (Update, Activate, Deactivate, Delete)
    public async Task<Area?> GetByIdTrackedAsync(int id, CancellationToken ct = default)
        => await _context.Areas
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    // NOTE: CountAsync and ToListAsync are two separate round trips with no transaction.
    // Areas is small reference data (< 500 rows) with rare writes — eventual consistency
    // on the pagination count is acceptable and documents this deliberate trade-off.
    public async Task<(IReadOnlyList<Area> Areas, int TotalCount)> GetAllAsync(int skip, int take, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Areas.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();
        if (regionId.HasValue) query = query.Where(a => a.RegionId == regionId.Value);
        if (isActive.HasValue) query = query.Where(a => a.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(a => EF.Functions.ILike(a.Name, pattern));
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

    public async Task<IReadOnlyList<AreaDto>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default)
    {
        // Global HasQueryFilter(x => x.IsActive) covers active-only restriction — no explicit Where needed.
        // Projecting to AreaDto here avoids materializing full entities + navigation objects for a dropdown.
        var query = _context.Areas.AsQueryable();
        if (regionId.HasValue) query = query.Where(a => a.RegionId == regionId.Value);
        return await query
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Take(1000)
            .Select(a => new AreaDto(
                a.Id,
                a.Name,
                a.RegionId,
                a.Region!.Name,
                a.IsActive,
                a.RowVersion,
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int regionId, CancellationToken ct = default)
        => await _context.Areas.IgnoreQueryFilters()
            .AnyAsync(a => EF.Functions.ILike(a.Name, name) && a.RegionId == regionId && !a.IsDeleted, ct);

    public async Task<bool> ExistsByNameAsync(string name, int regionId, int excludeId, CancellationToken ct = default)
        => await _context.Areas.IgnoreQueryFilters()
            .AnyAsync(a => EF.Functions.ILike(a.Name, name) && a.RegionId == regionId && a.Id != excludeId && !a.IsDeleted, ct);

    public async Task<bool> RegionExistsAsync(int regionId, CancellationToken ct = default)
        => await _context.Regions.IgnoreQueryFilters().AnyAsync(r => r.Id == regionId, ct);

    public async Task CreateAsync(Area area, CancellationToken ct = default)
        => await _context.Areas.AddAsync(area, ct);

    // Synchronous — CT not applicable; EF change-tracking is the mechanism, no async I/O here
    public Task UpdateAsync(Area area)
    {
        _context.Areas.Update(area);
        return Task.CompletedTask;
    }

    // Sets the OriginalValue of RowVersion so EF uses the client's version in the
    // WHERE xmin = $token clause — this is what actually detects cross-request staleness.
    // Assigning area.RowVersion directly only changes CurrentValue and has no effect on the lock.
    public void ApplyConcurrencyToken(Area area, uint rowVersion)
        => _context.Entry(area).Property(x => x.RowVersion).OriginalValue = rowVersion;

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
