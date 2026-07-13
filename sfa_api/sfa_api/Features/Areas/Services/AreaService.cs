using sfa_api.Common.Errors;
using sfa_api.Features.Areas.DTOs;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Areas.Repositories;
using sfa_api.Features.Areas.Requests;
using sfa_api.Features.GeoConsistency;
using sfa_api.Features.GeoConsistency.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Areas.Services;

public class AreaService(
    IAreaRepository repo,
    ICacheService cache,
    IGeoCascadeService cascade,
    ILogger<AreaService> logger) : IAreaService
{
    private readonly IAreaRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly IGeoCascadeService _cascade = cascade;
    private readonly ILogger<AreaService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ActiveCacheKey = "areas:active";
    private const string ListCachePrefix = "areas:list:";

    public async Task<AreaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Area", id);
        return MapToDto(area);
    }

    public async Task<AreaListDto> GetAllAsync(int page, int pageSize, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"areas:list:{page}:{pageSize}:{regionId}:{isActive}:{search}";
        var cached = await _cache.GetAsync<AreaListDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (areas, totalCount) = await _repo.GetAllAsync(skip, pageSize, regionId, isActive, search, ct);
        var result = new AreaListDto(
            Areas: areas.Select(MapToDto).ToList(),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<IReadOnlyList<AreaDto>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default)
    {
        var cacheKey = regionId.HasValue ? $"{ActiveCacheKey}:{regionId}" : ActiveCacheKey;
        var cached = await _cache.GetAsync<IReadOnlyList<AreaDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var result = await _repo.GetAllActiveAsync(regionId, ct);

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<AreaDto> CreateAsync(CreateAreaRequest request, int? callerId, CancellationToken ct = default)
    {
        if (!await _repo.RegionExistsAsync(request.RegionId, ct))
            throw new NotFoundException("Region", request.RegionId);

        if (await _repo.ExistsByNameAsync(request.Name, request.RegionId, ct))
            throw new DuplicateResourceException("Name");

        var area = new Area
        {
            Name = request.Name,
            RegionId = request.RegionId,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(area, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Area {AreaId} created by {CallerId}", area.Id, callerId);

        // Invalidate caches after write — prefix covers both "areas:active" and "areas:active:{regionId}" variants
        await _cache.RemoveByPrefixAsync(ActiveCacheKey, ct);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);

        // Re-fetch to populate navigation property (RegionName)
        var created = await _repo.GetByIdAsync(area.Id, ct)
            ?? throw new NotFoundException("Area", area.Id);
        return MapToDto(created);
    }

    public async Task<AreaDto> UpdateAsync(int id, UpdateAreaRequest request, int? callerId, CancellationToken ct = default)
    {
        // Use tracked fetch for mutation path
        var area = await _repo.GetByIdTrackedAsync(id, ct)
            ?? throw new NotFoundException("Area", id);

        if (!await _repo.RegionExistsAsync(request.RegionId, ct))
            throw new NotFoundException("Region", request.RegionId);

        if (await _repo.ExistsByNameAsync(request.Name, request.RegionId, id, ct))
            throw new DuplicateResourceException("Name");

        // Capture the pre-change parent so we only cascade on an actual region MOVE (not a rename).
        var oldRegionId = area.RegionId;

        // Tell EF to use the client's RowVersion as the OriginalValue in the WHERE xmin = $token clause.
        // Setting area.RowVersion directly only changes CurrentValue — OriginalValue is what EF checks.
        _repo.ApplyConcurrencyToken(area, request.RowVersion);
        area.Name = request.Name;
        area.RegionId = request.RegionId;
        area.UpdatedBy = callerId;
        area.UpdatedAt = DateTime.UtcNow;

        if (oldRegionId != request.RegionId)
        {
            // Re-parent: the area's RegionId is denormalized onto every live descendant
            // (territories → divisions → routes → outlets, plus distributors). Persist the area move
            // and fan the new RegionId down in ONE transaction so they can't diverge.
            await using var tx = await _repo.BeginTransactionAsync(ct);
            await _repo.UpdateAsync(area);
            await _repo.SaveChangesAsync(ct);   // area's own xmin concurrency check happens here
            var cascaded = await _cascade.CascadeAreaRegionChangeAsync(id, request.RegionId, ct);
            await tx.CommitAsync(ct);
            _logger.LogInformation(
                "Area {AreaId} moved from Region {OldRegionId} to {NewRegionId} by {CallerId}; cascaded {Count} descendant rows",
                id, oldRegionId, request.RegionId, callerId, cascaded);
            await InvalidateDescendantCachesAsync(ct);
        }
        else
        {
            await _repo.UpdateAsync(area);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Area {AreaId} updated by {CallerId}", id, callerId);
        }

        // Invalidate caches after write — prefix covers both "areas:active" and "areas:active:{regionId}" variants
        await _cache.RemoveByPrefixAsync(ActiveCacheKey, ct);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);

        // Re-fetch to populate navigation property
        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Area", id);
        return MapToDto(updated);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdTrackedAsync(id, ct)
            ?? throw new NotFoundException("Area", id);

        var wasActive = area.IsActive;
        area.IsActive = true;
        area.UpdatedBy = callerId;
        area.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(area);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Area {AreaId} status changed from {OldStatus} to {NewStatus} by {CallerId}",
            id, wasActive ? "Active" : "Inactive", "Active", callerId);

        await _cache.RemoveByPrefixAsync(ActiveCacheKey, ct);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdTrackedAsync(id, ct)
            ?? throw new NotFoundException("Area", id);

        // Integrity guard: deactivating a parent with active children would leave them
        // orphaned under an inactive area. Block it, same as delete.
        if (await _repo.HasActiveTerritoriesAsync(id, ct))
            throw new BusinessRuleException(
                "AREA_HAS_ACTIVE_TERRITORIES",
                "Cannot deactivate an area that still has active territories. Deactivate or move them first.",
                new { areaId = id });

        var wasActive = area.IsActive;
        area.IsActive = false;
        area.UpdatedBy = callerId;
        area.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(area);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Area {AreaId} status changed from {OldStatus} to {NewStatus} by {CallerId}",
            id, wasActive ? "Active" : "Inactive", "Inactive", callerId);

        await _cache.RemoveByPrefixAsync(ActiveCacheKey, ct);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdTrackedAsync(id, ct)
            ?? throw new NotFoundException("Area", id);

        // Integrity guard: refuse to delete a parent that still has active children.
        if (await _repo.HasActiveTerritoriesAsync(id, ct))
            throw new BusinessRuleException(
                "AREA_HAS_ACTIVE_TERRITORIES",
                "Cannot delete an area that still has active territories. Deactivate or move them first.",
                new { areaId = id });

        area.IsActive = false;
        area.IsDeleted = true;
        area.UpdatedBy = callerId;
        area.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(area);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Area {AreaId} deleted by {CallerId}", id, callerId);

        await _cache.RemoveByPrefixAsync(ActiveCacheKey, ct);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    // Clears the live descendants' list caches after a re-parent cascade so no stale region survives
    // in a cached territory/division/distributor/outlet list.
    private async Task InvalidateDescendantCachesAsync(CancellationToken ct)
    {
        foreach (var prefix in GeoCacheKeys.DescendantListPrefixes)
            await _cache.RemoveByPrefixAsync(prefix, ct);
    }

    private static AreaDto MapToDto(Area area) => new(
        Id: area.Id,
        Name: area.Name,
        RegionId: area.RegionId,
        RegionName: area.Region?.Name ?? string.Empty,
        IsActive: area.IsActive,
        RowVersion: area.RowVersion,
        CreatedAt: area.CreatedAt,
        UpdatedAt: area.UpdatedAt
    );
}
