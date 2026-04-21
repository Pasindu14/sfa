using sfa_api.Common.Errors;
using sfa_api.Features.Fleets.DTOs;
using sfa_api.Features.Fleets.Entities;
using sfa_api.Features.Fleets.Repositories;
using sfa_api.Features.Fleets.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Fleets.Services;

public class FleetService(
    IFleetRepository repo,
    ICacheService cache,
    ILogger<FleetService> logger) : IFleetService
{
    private readonly IFleetRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<FleetService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCachePrefix = "fleets:list:";
    private const string AllActiveCacheKey = "fleets:all-active";

    public async Task<FleetDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var fleet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Fleet", id);
        return MapToDto(fleet);
    }

    public async Task<FleetListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"{ListCachePrefix}{page}:{pageSize}:{search}";
        var cached = await _cache.GetAsync<FleetListDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (fleets, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
        var result = new FleetListDto(
            Fleets: fleets.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<IEnumerable<FleetDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<IEnumerable<FleetDto>>(AllActiveCacheKey, ct);
        if (cached is not null) return cached;

        var fleets = await _repo.GetAllActiveAsync(ct);
        var result = fleets.Select(MapToDto).ToList();

        await _cache.SetAsync(AllActiveCacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<FleetDto> CreateAsync(CreateFleetRequest request, int? callerId, CancellationToken ct = default)
    {
        if (await _repo.ExistsByNameAsync(request.Name, ct))
            throw new DuplicateResourceException("Name");

        var fleet = new Fleet
        {
            Name = request.Name,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(fleet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Fleet {FleetId} created", fleet.Id);
        await InvalidateCacheAsync(ct);
        return MapToDto(fleet);
    }

    public async Task<FleetDto> UpdateAsync(int id, UpdateFleetRequest request, int? callerId, CancellationToken ct = default)
    {
        var fleet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Fleet", id);

        if (await _repo.ExistsByNameAsync(request.Name, id, ct))
            throw new DuplicateResourceException("Name");

        fleet.Name = request.Name;
        fleet.UpdatedBy = callerId;
        fleet.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(fleet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Fleet {FleetId} updated", id);
        await InvalidateCacheAsync(ct);
        return MapToDto(fleet);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var fleet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Fleet", id);

        fleet.IsActive = true;
        fleet.UpdatedBy = callerId;
        fleet.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(fleet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Fleet {FleetId} activated", id);
        await InvalidateCacheAsync(ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var fleet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Fleet", id);

        fleet.IsActive = false;
        fleet.UpdatedBy = callerId;
        fleet.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(fleet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Fleet {FleetId} deactivated", id);
        await InvalidateCacheAsync(ct);
    }

    private async Task InvalidateCacheAsync(CancellationToken ct)
    {
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        await _cache.RemoveAsync(AllActiveCacheKey, ct);
    }

    private static FleetDto MapToDto(Fleet fleet) => new(
        Id: fleet.Id,
        Name: fleet.Name,
        IsActive: fleet.IsActive,
        CreatedAt: fleet.CreatedAt,
        UpdatedAt: fleet.UpdatedAt
    );
}
