using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
            // Search by area name, parent region name, or exact "code" (numeric Id).
            // Name/region substring matches ride the pg_trgm GIN indexes; the code branch
            // is an exact PK lookup (parse to int) rather than a CAST+ILIKE seq-scan.
            var pattern = $"%{search}%";
            var isCode = int.TryParse(search.Trim(), out var codeId);
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(a => EF.Functions.ILike(a.Name, pattern)
                                   || EF.Functions.ILike(a.Region!.Name, pattern)
                                   || (isCode && a.Id == codeId))
                : query.Where(a => EF.Functions.Like(a.Name, pattern)
                                   || EF.Functions.Like(a.Region!.Name, pattern)
                                   || (isCode && a.Id == codeId));
        }

        var totalCount = await query.CountAsync(ct);
        var areas = await query
            .Include(a => a.Region)
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Skip(skip)
            .Take(take).ToListAsync(ct);
        return (areas, totalCount);
    }

    public async Task<IReadOnlyList<AreaDto>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default)
    {
        // Global HasQueryFilter(x => x.IsActive && !x.IsDeleted) covers active-only and soft-delete restriction — no explicit Where needed.
        // Projecting to AreaDto here avoids materializing full entities + navigation objects for a dropdown.
        var query = _context.Areas.AsQueryable();
        if (regionId.HasValue) query = query.Where(a => a.RegionId == regionId.Value);
        return await query
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AreaDto(
                a.Id,
                a.Name,
                a.RegionId,
                a.Region!.Name,
                a.IsActive,
                a.RowVersion,
                a.CreatedAt,
                a.UpdatedAt))
            .Take(1000).ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int regionId, CancellationToken ct = default)
    {
        var query = _context.Areas.IgnoreQueryFilters().Where(a => a.RegionId == regionId && !a.IsDeleted);
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(a => EF.Functions.ILike(a.Name, name))
            : query.Where(a => EF.Functions.Like(a.Name, name));
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int regionId, int excludeId, CancellationToken ct = default)
    {
        var query = _context.Areas.IgnoreQueryFilters().Where(a => a.RegionId == regionId && a.Id != excludeId && !a.IsDeleted);
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(a => EF.Functions.ILike(a.Name, name))
            : query.Where(a => EF.Functions.Like(a.Name, name));
        return await query.AnyAsync(ct);
    }

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

    public async Task<bool> HasActiveTerritoriesAsync(int areaId, CancellationToken ct = default)
        => await _context.Territories
            .IgnoreQueryFilters()
            .AnyAsync(t => t.AreaId == areaId && t.IsActive && !t.IsDeleted, ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _context.Database.BeginTransactionAsync(ct);

    public IExecutionStrategy CreateExecutionStrategy()
        => _context.Database.CreateExecutionStrategy();

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
