using sfa_api.Common.Errors;
using sfa_api.Features.Areas.DTOs;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Areas.Repositories;
using sfa_api.Features.Areas.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Areas.Services;

public class AreaService(
    IAreaRepository repo,
    ICacheService cache,
    ILogger<AreaService> logger) : IAreaService
{
    private readonly IAreaRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<AreaService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<AreaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Area", id);
        return MapToDto(area);
    }

    public async Task<AreaListDto> GetAllAsync(int page, int pageSize, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"areas:list:{page}:{pageSize}:{regionId}:{isActive}:{search}";
        var cached = await _cache.GetAsync<AreaListDto>(cacheKey);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (areas, totalCount) = await _repo.GetAllAsync(skip, pageSize, regionId, isActive, search, ct);
        var result = new AreaListDto(
            Areas: areas.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<IEnumerable<AreaDto>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default)
    {
        var areas = await _repo.GetAllActiveAsync(regionId, ct);
        return areas.Select(MapToDto);
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

        _logger.LogInformation("Area {AreaId} created", area.Id);

        // Re-fetch to populate navigation property
        var created = await _repo.GetByIdAsync(area.Id, ct)
            ?? throw new NotFoundException("Area", area.Id);
        return MapToDto(created);
    }

    public async Task<AreaDto> UpdateAsync(int id, UpdateAreaRequest request, int? callerId, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Area", id);

        if (!await _repo.RegionExistsAsync(request.RegionId, ct))
            throw new NotFoundException("Region", request.RegionId);

        if (await _repo.ExistsByNameAsync(request.Name, request.RegionId, id, ct))
            throw new DuplicateResourceException("Name");

        area.Name = request.Name;
        area.RegionId = request.RegionId;
        area.UpdatedBy = callerId;
        area.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(area, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Area {AreaId} updated", id);

        // Re-fetch to populate navigation property
        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Area", id);
        return MapToDto(updated);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Area", id);

        area.IsActive = true;
        area.UpdatedBy = callerId;
        area.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(area, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Area {AreaId} activated", id);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var area = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Area", id);

        area.IsActive = false;
        area.UpdatedBy = callerId;
        area.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(area, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Area {AreaId} deactivated", id);
    }

    private static AreaDto MapToDto(Area area) => new(
        Id: area.Id,
        Name: area.Name,
        RegionId: area.RegionId,
        RegionName: area.Region?.Name ?? string.Empty,
        IsActive: area.IsActive,
        CreatedAt: area.CreatedAt,
        UpdatedAt: area.UpdatedAt
    );
}
