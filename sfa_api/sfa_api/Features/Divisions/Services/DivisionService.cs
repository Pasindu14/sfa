using sfa_api.Common.Errors;
using sfa_api.Features.Divisions.DTOs;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Divisions.Repositories;
using sfa_api.Features.Divisions.Requests;
using sfa_api.Features.GeoConsistency;
using sfa_api.Features.GeoConsistency.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Divisions.Services;

public class DivisionService(
    IDivisionRepository repo,
    ICacheService cache,
    IGeoCascadeService cascade,
    ILogger<DivisionService> logger) : IDivisionService
{
    private readonly IDivisionRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly IGeoCascadeService _cascade = cascade;
    private readonly ILogger<DivisionService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCachePrefix = "divisions:list:";

    public async Task<DivisionDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var division = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Division", id);
        return MapToDto(division);
    }

    public async Task<DivisionListDto> GetAllAsync(int page, int pageSize, int? territoryId = null, int? areaId = null, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"divisions:list:{page}:{pageSize}:{territoryId}:{areaId}:{regionId}:{isActive}:{search}";
        var cached = await _cache.GetAsync<DivisionListDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (divisions, totalCount) = await _repo.GetAllAsync(skip, pageSize, territoryId, areaId, regionId, isActive, search, ct);
        var result = new DivisionListDto(
            Divisions: divisions.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<IEnumerable<DivisionDto>> GetAllActiveAsync(int? territoryId = null, CancellationToken ct = default)
    {
        var divisions = await _repo.GetAllActiveAsync(territoryId, ct);
        return divisions.Select(MapToDto);
    }

    public async Task<DivisionDto> CreateAsync(CreateDivisionRequest request, int? callerId, CancellationToken ct = default)
    {
        var territory = await _repo.GetTerritoryWithAncestorsAsync(request.TerritoryId, ct)
            ?? throw new NotFoundException("Territory", request.TerritoryId);

        if (await _repo.ExistsByNameAsync(request.Name, request.TerritoryId, ct))
            throw new DuplicateResourceException("Name");

        var division = new Division
        {
            Name = request.Name,
            TerritoryId = territory.Id,
            AreaId = territory.AreaId,
            RegionId = territory.RegionId,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(division, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Division {DivisionId} created", division.Id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);

        var created = await _repo.GetByIdAsync(division.Id, ct)
            ?? throw new NotFoundException("Division", division.Id);
        return MapToDto(created);
    }

    public async Task<DivisionDto> UpdateAsync(int id, UpdateDivisionRequest request, int? callerId, CancellationToken ct = default)
    {
        var division = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Division", id);

        var territory = await _repo.GetTerritoryWithAncestorsAsync(request.TerritoryId, ct)
            ?? throw new NotFoundException("Territory", request.TerritoryId);

        if (await _repo.ExistsByNameAsync(request.Name, request.TerritoryId, id, ct))
            throw new DuplicateResourceException("Name");

        // Capture the pre-change parent so we only cascade on an actual territory MOVE (not a rename).
        var oldTerritoryId = division.TerritoryId;

        _repo.ApplyConcurrencyToken(division, request.RowVersion);
        division.Name = request.Name;
        division.TerritoryId = territory.Id;
        division.AreaId = territory.AreaId;
        division.RegionId = territory.RegionId;
        division.UpdatedBy = callerId;
        division.UpdatedAt = DateTime.UtcNow;

        if (oldTerritoryId != territory.Id)
        {
            // Re-parent: the division's TerritoryId/AreaId/RegionId are denormalized onto its live
            // descendants (routes → outlets). Persist the move and fan the new chain down atomically.
            await using var tx = await _repo.BeginTransactionAsync(ct);
            await _repo.UpdateAsync(division, ct);
            await _repo.SaveChangesAsync(ct);   // division's own xmin concurrency check happens here
            var cascaded = await _cascade.CascadeDivisionTerritoryChangeAsync(
                id, territory.Id, territory.AreaId, territory.RegionId, ct);
            await tx.CommitAsync(ct);
            _logger.LogInformation(
                "Division {DivisionId} moved from Territory {OldTerritoryId} to {NewTerritoryId}; cascaded {Count} descendant rows",
                id, oldTerritoryId, territory.Id, cascaded);
            await InvalidateDescendantCachesAsync(ct);
        }
        else
        {
            await _repo.UpdateAsync(division, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Division {DivisionId} updated", id);
        }

        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);

        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Division", id);
        return MapToDto(updated);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var division = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Division", id);

        division.IsActive = true;
        division.UpdatedBy = callerId;
        division.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(division, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Division {DivisionId} activated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var division = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Division", id);

        division.IsActive = false;
        division.UpdatedBy = callerId;
        division.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(division, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Division {DivisionId} deactivated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var division = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Division", id);

        // Soft-delete: IsDeleted is the audit flag for an explicit delete, distinct from
        // deactivate (IsActive = false). Never hard-delete.
        division.IsActive = false;
        division.IsDeleted = true;
        division.UpdatedBy = callerId;
        division.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(division, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Division {DivisionId} deleted by {CallerId}", id, callerId);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    // Clears the live descendants' list caches after a re-parent cascade.
    private async Task InvalidateDescendantCachesAsync(CancellationToken ct)
    {
        foreach (var prefix in GeoCacheKeys.DescendantListPrefixes)
            await _cache.RemoveByPrefixAsync(prefix, ct);
    }

    private static DivisionDto MapToDto(Division d) => new(
        Id: d.Id,
        Name: d.Name,
        TerritoryId: d.TerritoryId,
        TerritoryName: d.Territory?.Name ?? string.Empty,
        AreaId: d.AreaId,
        AreaName: d.Area?.Name ?? d.Territory?.Area?.Name ?? string.Empty,
        RegionId: d.RegionId,
        RegionName: d.Region?.Name ?? d.Territory?.Area?.Region?.Name ?? string.Empty,
        IsActive: d.IsActive,
        RowVersion: d.RowVersion,
        CreatedAt: d.CreatedAt,
        UpdatedAt: d.UpdatedAt
    );
}
