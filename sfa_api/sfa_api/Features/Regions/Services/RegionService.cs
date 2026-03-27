using sfa_api.Common.Errors;
using sfa_api.Features.Regions.DTOs;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Regions.Repositories;
using sfa_api.Features.Regions.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Regions.Services;

public class RegionService(
    IRegionRepository repo,
    ICacheService cache,
    ILogger<RegionService> logger) : IRegionService
{
    private readonly IRegionRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<RegionService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<RegionDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var region = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Region", id);
        return MapToDto(region);
    }

    public async Task<RegionListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"regions:list:{page}:{pageSize}:{search}";
        var cached = await _cache.GetAsync<RegionListDto>(cacheKey);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (regions, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
        var result = new RegionListDto(
            Regions: regions.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<IEnumerable<RegionDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var regions = await _repo.GetAllActiveAsync(ct);
        return regions.Select(MapToDto);
    }

    public async Task<RegionDto> CreateAsync(CreateRegionRequest request, int? callerId, CancellationToken ct = default)
    {
        if (await _repo.ExistsByNameAsync(request.Name, ct))
            throw new DuplicateResourceException("Name");

        var region = new Region
        {
            Name = request.Name,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(region, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Region {RegionId} created", region.Id);
        return MapToDto(region);
    }

    public async Task<RegionDto> UpdateAsync(int id, UpdateRegionRequest request, int? callerId, CancellationToken ct = default)
    {
        var region = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Region", id);

        if (await _repo.ExistsByNameAsync(request.Name, id, ct))
            throw new DuplicateResourceException("Name");

        region.Name = request.Name;
        region.UpdatedBy = callerId;
        region.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(region, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Region {RegionId} updated", id);
        return MapToDto(region);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var region = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Region", id);

        region.IsActive = true;
        region.UpdatedBy = callerId;
        region.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(region, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Region {RegionId} activated", id);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var region = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Region", id);

        region.IsActive = false;
        region.UpdatedBy = callerId;
        region.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(region, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Region {RegionId} deactivated", id);
    }

    private static RegionDto MapToDto(Region region) => new(
        Id: region.Id,
        Name: region.Name,
        IsActive: region.IsActive,
        CreatedAt: region.CreatedAt,
        UpdatedAt: region.UpdatedAt
    );
}
