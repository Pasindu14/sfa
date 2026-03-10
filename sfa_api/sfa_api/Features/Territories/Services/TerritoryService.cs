using sfa_api.Common.Errors;
using sfa_api.Features.Territories.DTOs;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Territories.Repositories;
using sfa_api.Features.Territories.Requests;

namespace sfa_api.Features.Territories.Services;

public class TerritoryService(
    ITerritoryRepository repo,
    ILogger<TerritoryService> logger) : ITerritoryService
{
    private readonly ITerritoryRepository _repo = repo;
    private readonly ILogger<TerritoryService> _logger = logger;

    public async Task<TerritoryDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var territory = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);
        return MapToDto(territory);
    }

    public async Task<TerritoryListDto> GetAllAsync(int page, int pageSize, int? areaId = null, bool? isActive = null, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (territories, totalCount) = await _repo.GetAllAsync(skip, pageSize, areaId, isActive, ct);
        return new TerritoryListDto(
            Territories: territories.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
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

        territory.Name = request.Name;
        territory.AreaId = area.Id;
        territory.RegionId = area.RegionId;
        territory.UpdatedBy = callerId;
        territory.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(territory, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Territory {TerritoryId} updated", id);

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
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var territory = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Territory", id);

        territory.IsActive = false;
        territory.UpdatedBy = callerId;
        territory.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(territory, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Territory {TerritoryId} deactivated", id);
    }

    private static TerritoryDto MapToDto(Territory territory) => new(
        Id: territory.Id,
        Name: territory.Name,
        AreaId: territory.AreaId,
        AreaName: territory.Area?.Name ?? string.Empty,
        RegionId: territory.RegionId,
        RegionName: territory.Region?.Name ?? territory.Area?.Region?.Name ?? string.Empty,
        IsActive: territory.IsActive,
        CreatedAt: territory.CreatedAt,
        UpdatedAt: territory.UpdatedAt
    );
}
