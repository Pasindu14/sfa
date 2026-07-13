using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.GeoConsistency;
using sfa_api.Features.GeoConsistency.Services;
using sfa_api.Features.Territories.DTOs;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Territories.Repositories;
using sfa_api.Features.Territories.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Territories.Services;

public class TerritoryService(
    ITerritoryRepository repo,
    ICacheService cache,
    IGeoCascadeService cascade,
    ILogger<TerritoryService> logger) : ITerritoryService
{
    private readonly ITerritoryRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly IGeoCascadeService _cascade = cascade;
    private readonly ILogger<TerritoryService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCachePrefix = "territories:list:";

    public async Task<TerritoryDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var territory = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);
        return MapToDto(territory);
    }

    public async Task<TerritoryListDto> GetAllAsync(int page, int pageSize, int? areaId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"territories:list:{page}:{pageSize}:{areaId}:{isActive}:{search}";
        var cached = await _cache.GetAsync<TerritoryListDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (territories, totalCount) = await _repo.GetAllAsync(skip, pageSize, areaId, isActive, search, ct);
        var result = new TerritoryListDto(
            Territories: territories.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<IEnumerable<TerritoryDto>> GetAllActiveAsync(int? areaId = null, CancellationToken ct = default)
    {
        var territories = await _repo.GetAllActiveAsync(areaId, ct);
        return territories.Select(MapToDto);
    }

    public async Task<TerritoryDto> CreateAsync(CreateTerritoryRequest request, int? callerId, CancellationToken ct = default)
    {
        var area = await _repo.GetAreaWithRegionAsync(request.AreaId, ct)
            ?? throw new NotFoundException("Area", request.AreaId);

        if (await _repo.ExistsByNameAsync(request.Name, request.AreaId, ct))
            throw new DuplicateResourceException("Name");

        var territory = new Territory
        {
            Name = request.Name,
            AreaId = area.Id,
            RegionId = area.RegionId,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(territory, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Territory {TerritoryId} created", territory.Id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);

        var created = await _repo.GetByIdAsync(territory.Id, ct)
            ?? throw new NotFoundException("Territory", territory.Id);
        return MapToDto(created);
    }

    public async Task<TerritoryDto> UpdateAsync(int id, UpdateTerritoryRequest request, int? callerId, CancellationToken ct = default)
    {
        var territory = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);

        var area = await _repo.GetAreaWithRegionAsync(request.AreaId, ct)
            ?? throw new NotFoundException("Area", request.AreaId);

        if (await _repo.ExistsByNameAsync(request.Name, request.AreaId, id, ct))
            throw new DuplicateResourceException("Name");

        // Capture the pre-change parent so we only cascade on an actual area MOVE (not a rename).
        var oldAreaId = territory.AreaId;

        _repo.ApplyConcurrencyToken(territory, request.RowVersion);
        territory.Name = request.Name;
        territory.AreaId = area.Id;
        territory.RegionId = area.RegionId;
        territory.UpdatedBy = callerId;
        territory.UpdatedAt = DateTime.UtcNow;

        if (oldAreaId != area.Id)
        {
            // Re-parent: the territory's AreaId/RegionId are denormalized onto every live descendant
            // (divisions → routes → outlets, plus distributors). Persist the move and fan the new
            // AreaId + RegionId down in ONE transaction, wrapped in an execution strategy because
            // EnableRetryOnFailure is on.
            var cascaded = 0;
            var strategy = _repo.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _repo.BeginTransactionAsync(ct);
                await _repo.UpdateAsync(territory, ct);
                await _repo.SaveChangesAsync(ct);   // territory's own xmin concurrency check happens here
                cascaded = await _cascade.CascadeTerritoryAreaChangeAsync(id, area.Id, area.RegionId, ct);
                await tx.CommitAsync(ct);
            });
            _logger.LogInformation(
                "Territory {TerritoryId} moved from Area {OldAreaId} to {NewAreaId} (Region {RegionId}); cascaded {Count} descendant rows",
                id, oldAreaId, area.Id, area.RegionId, cascaded);
            await InvalidateDescendantCachesAsync(ct);
        }
        else
        {
            await _repo.UpdateAsync(territory, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Territory {TerritoryId} updated", id);
        }

        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);

        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);
        return MapToDto(updated);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var territory = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);

        territory.IsActive = true;
        territory.UpdatedBy = callerId;
        territory.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(territory, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Territory {TerritoryId} activated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var territory = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);

        // Integrity guard: deactivating a parent with active children would leave them
        // orphaned under an inactive territory. Block it, same as delete.
        if (await _repo.HasActiveDivisionsAsync(id, ct))
            throw new BusinessRuleException(
                "TERRITORY_HAS_ACTIVE_DIVISIONS",
                "Cannot deactivate a territory that still has active divisions. Deactivate or move them first.",
                new { territoryId = id });

        territory.IsActive = false;
        territory.UpdatedBy = callerId;
        territory.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(territory, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Territory {TerritoryId} deactivated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var territory = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);

        // Integrity guard: refuse to delete a parent that still has active children.
        if (await _repo.HasActiveDivisionsAsync(id, ct))
            throw new BusinessRuleException(
                "TERRITORY_HAS_ACTIVE_DIVISIONS",
                "Cannot delete a territory that still has active divisions. Deactivate or move them first.",
                new { territoryId = id });

        // Soft-delete: IsDeleted is the audit flag for an explicit delete, distinct from
        // deactivate (IsActive = false). Never hard-delete.
        territory.IsActive = false;
        territory.IsDeleted = true;
        territory.UpdatedBy = callerId;
        territory.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(territory, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Territory {TerritoryId} deleted by {CallerId}", id, callerId);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    // Clears the live descendants' list caches after a re-parent cascade.
    private async Task InvalidateDescendantCachesAsync(CancellationToken ct)
    {
        foreach (var prefix in GeoCacheKeys.DescendantListPrefixes)
            await _cache.RemoveByPrefixAsync(prefix, ct);
    }

    private static TerritoryDto MapToDto(Territory territory) => new(
        Id: territory.Id,
        Name: territory.Name,
        AreaId: territory.AreaId,
        AreaName: territory.Area?.Name ?? string.Empty,
        RegionId: territory.RegionId,
        RegionName: territory.Region?.Name ?? territory.Area?.Region?.Name ?? string.Empty,
        IsActive: territory.IsActive,
        RowVersion: territory.RowVersion,
        CreatedAt: territory.CreatedAt,
        UpdatedAt: territory.UpdatedAt
    );
}
