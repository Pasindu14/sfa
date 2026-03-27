using sfa_api.Common.Errors;
using sfa_api.Features.Divisions.DTOs;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Divisions.Repositories;
using sfa_api.Features.Divisions.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Divisions.Services;

public class DivisionService(
    IDivisionRepository repo,
    ICacheService cache,
    ILogger<DivisionService> logger) : IDivisionService
{
    private readonly IDivisionRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<DivisionService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<DivisionDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var division = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Division", id);
        return MapToDto(division);
    }

    public async Task<DivisionListDto> GetAllAsync(int page, int pageSize, int? territoryId = null, int? areaId = null, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"divisions:list:{page}:{pageSize}:{territoryId}:{areaId}:{regionId}:{isActive}:{search}";
        var cached = await _cache.GetAsync<DivisionListDto>(cacheKey);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (divisions, totalCount) = await _repo.GetAllAsync(skip, pageSize, territoryId, areaId, regionId, isActive, search, ct);
        var result = new DivisionListDto(
            Divisions: divisions.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl);
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

        division.Name = request.Name;
        division.TerritoryId = territory.Id;
        division.AreaId = territory.AreaId;
        division.RegionId = territory.RegionId;
        division.UpdatedBy = callerId;
        division.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(division, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Division {DivisionId} updated", id);

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
        CreatedAt: d.CreatedAt,
        UpdatedAt: d.UpdatedAt
    );
}
