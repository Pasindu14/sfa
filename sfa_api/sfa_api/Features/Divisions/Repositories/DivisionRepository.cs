using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Common.Errors;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Divisions.Repositories;

public class DivisionRepository(AppDbContext context) : IDivisionRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Division?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Divisions
            .IgnoreQueryFilters()
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IEnumerable<Division> Divisions, int TotalCount)> GetAllAsync(
        int skip, int take,
        int? territoryId = null, int? areaId = null, int? regionId = null,
        bool? isActive = null, string? search = null,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Divisions.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();
        if (territoryId.HasValue) query = query.Where(d => d.TerritoryId == territoryId.Value);
        if (areaId.HasValue) query = query.Where(d => d.AreaId == areaId.Value);
        if (regionId.HasValue) query = query.Where(d => d.RegionId == regionId.Value);
        if (isActive.HasValue) query = query.Where(d => d.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Search by division name, parent territory name, or exact "code" (numeric Id).
            // Name/territory substring matches ride the pg_trgm GIN indexes
            // (IX_Divisions_Name_Trgm / IX_Territories_Name_Trgm); the code branch is an
            // exact PK lookup (parse to int) rather than a CAST+ILIKE that would seq-scan.
            var pattern = $"%{search}%";
            var isCode = int.TryParse(search.Trim(), out var codeId);
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(d => EF.Functions.ILike(d.Name, pattern)
                                   || EF.Functions.ILike(d.Territory!.Name, pattern)
                                   || (isCode && d.Id == codeId))
                : query.Where(d => EF.Functions.Like(d.Name, pattern)
                                   || EF.Functions.Like(d.Territory!.Name, pattern)
                                   || (isCode && d.Id == codeId));
        }

        var totalCount = await query.CountAsync(ct);
        var divisions = await query
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (divisions, totalCount);
    }

    public async Task<IEnumerable<Division>> GetAllActiveAsync(int? territoryId = null, CancellationToken ct = default)
    {
        var query = _context.Divisions.Where(d => d.IsActive);
        if (territoryId.HasValue) query = query.Where(d => d.TerritoryId == territoryId.Value);
        return await query
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Take(500)
            .ToListAsync(ct);
    }

    public async Task<Territory?> GetTerritoryWithAncestorsAsync(int territoryId, CancellationToken ct = default)
        => await _context.Territories
            .IgnoreQueryFilters()
            .Include(t => t.Area)
                .ThenInclude(a => a!.Region)
            .FirstOrDefaultAsync(t => t.Id == territoryId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int territoryId, CancellationToken ct = default)
    {
        var query = _context.Divisions.IgnoreQueryFilters().Where(d => d.TerritoryId == territoryId);
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(d => EF.Functions.ILike(d.Name, name))   // case-insensitive duplicate check
            : query.Where(d => EF.Functions.Like(d.Name, name));
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int territoryId, int excludeId, CancellationToken ct = default)
    {
        var query = _context.Divisions.IgnoreQueryFilters().Where(d => d.TerritoryId == territoryId && d.Id != excludeId);
        query = _context.Database.ProviderName?.Contains("Npgsql") == true
            ? query.Where(d => EF.Functions.ILike(d.Name, name))
            : query.Where(d => EF.Functions.Like(d.Name, name));
        return await query.AnyAsync(ct);
    }

    public async Task<bool> TerritoryExistsAsync(int territoryId, CancellationToken ct = default)
        => await _context.Territories.IgnoreQueryFilters().AnyAsync(t => t.Id == territoryId, ct);

    public async Task CreateAsync(Division division, CancellationToken ct = default)
        => await _context.Divisions.AddAsync(division, ct);

    public Task UpdateAsync(Division division, CancellationToken ct = default)
    {
        _context.Divisions.Update(division);
        return Task.CompletedTask;
    }

    // Sets the OriginalValue of RowVersion so EF uses the client's version in the
    // WHERE xmin = $token clause — this is what detects cross-request staleness.
    public void ApplyConcurrencyToken(Division division, uint rowVersion)
        => _context.Entry(division).Property(x => x.RowVersion).OriginalValue = rowVersion;

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
