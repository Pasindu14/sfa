using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.DTOs;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Distributors.Requests;
using sfa_api.Features.Fleets.Repositories;
using sfa_api.Features.Territories.Repositories;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;

namespace sfa_api.Features.Distributors.Services;

public class DistributorService(
    IDistributorRepository repo,
    ITerritoryRepository territoryRepo,
    IFleetRepository fleetRepo,
    ICacheService cache,
    IDistributedLockService lockService,
    ILogger<DistributorService> logger) : IDistributorService
{
    private readonly IDistributorRepository _repo = repo;
    private readonly ITerritoryRepository _territoryRepo = territoryRepo;
    private readonly IFleetRepository _fleetRepo = fleetRepo;
    private readonly ICacheService _cache = cache;
    private readonly IDistributedLockService _lockService = lockService;
    private readonly ILogger<DistributorService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCachePrefix = "distributors:list:";

    public async Task<DistributorDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);
        return MapToDto(distributor);
    }

    public async Task<DistributorListDto> GetAllAsync(int page, int pageSize, string? search = null, bool? isActive = null, CancellationToken ct = default)
    {
        (page, pageSize) = sfa_api.Common.Extensions.PaginationHelper.Clamp(page, pageSize);
        var cacheKey = $"distributors:list:{page}:{pageSize}:{search}:{isActive}";
        var cached = await _cache.GetAsync<DistributorListDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (distributors, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, isActive, ct);
        var result = new DistributorListDto(
            Distributors: distributors.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<DistributorDto> CreateAsync(CreateDistributorRequest request, int? callerId, CancellationToken ct = default)
    {
        await using var advisoryLock = await _lockService.AcquireAsync($"distributor:create:{request.Email}", ct)
            ?? throw new ConcurrencyConflictException(new { email = request.Email, message = "A concurrent distributor creation with this email is already in progress." });

        if (await _repo.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateResourceException("Email");

        if (await _repo.ExistsByPhoneAsync(request.Phone, ct))
            throw new DuplicateResourceException("Phone");

        int? territoryId = null, areaId = null, regionId = null;
        if (request.TerritoryId.HasValue)
        {
            if (await _repo.ExistsByTerritoryIdAsync(request.TerritoryId.Value, ct))
                throw new DuplicateResourceException("TerritoryId");

            var territory = await _territoryRepo.GetByIdAsync(request.TerritoryId.Value, ct)
                ?? throw new NotFoundException("Territory", request.TerritoryId.Value);
            territoryId = territory.Id;
            areaId = territory.AreaId;
            regionId = territory.RegionId;
        }

        if (request.FleetId.HasValue && !await _fleetRepo.ExistsByIdAsync(request.FleetId.Value, ct))
            throw new NotFoundException("Fleet", request.FleetId.Value);

        var distributor = new Distributor
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            Alias = request.Alias,
            TradeDiscount = request.TradeDiscount,
            Commission = request.Commission,
            Category = request.Category,
            Remark = request.Remark,
            VatRegNo = request.VatRegNo,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TerritoryId = territoryId,
            AreaId = areaId,
            RegionId = regionId,
            FleetId = request.FleetId,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} created", distributor.Id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        return MapToDto(distributor);
    }

    public async Task<DistributorDto> UpdateAsync(int id, UpdateDistributorRequest request, int? callerId, CancellationToken ct = default)
    {
        await using var advisoryLock = await _lockService.AcquireAsync($"distributor:update:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { distributorId = id, message = "A concurrent update for this distributor is already in progress." });

        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        if (await _repo.ExistsByEmailAsync(request.Email, id, ct))
            throw new DuplicateResourceException("Email");

        if (await _repo.ExistsByPhoneAsync(request.Phone, id, ct))
            throw new DuplicateResourceException("Phone");

        if (request.TerritoryId != distributor.TerritoryId)
        {
            if (request.TerritoryId.HasValue)
            {
                if (await _repo.ExistsByTerritoryIdAsync(request.TerritoryId.Value, id, ct))
                    throw new DuplicateResourceException("TerritoryId");

                var territory = await _territoryRepo.GetByIdAsync(request.TerritoryId.Value, ct)
                    ?? throw new NotFoundException("Territory", request.TerritoryId.Value);
                distributor.TerritoryId = territory.Id;
                distributor.AreaId = territory.AreaId;
                distributor.RegionId = territory.RegionId;
            }
            else
            {
                distributor.TerritoryId = null;
                distributor.AreaId = null;
                distributor.RegionId = null;
            }
        }

        if (request.FleetId != distributor.FleetId)
        {
            if (request.FleetId.HasValue && !await _fleetRepo.ExistsByIdAsync(request.FleetId.Value, ct))
                throw new NotFoundException("Fleet", request.FleetId.Value);
            distributor.FleetId = request.FleetId;
        }

        distributor.Name = request.Name;
        distributor.Address = request.Address;
        distributor.Phone = request.Phone;
        distributor.Email = request.Email;
        distributor.Alias = request.Alias;
        distributor.TradeDiscount = request.TradeDiscount;
        distributor.Commission = request.Commission;
        distributor.Category = request.Category;
        distributor.Remark = request.Remark;
        distributor.VatRegNo = request.VatRegNo;
        distributor.Latitude = request.Latitude;
        distributor.Longitude = request.Longitude;
        distributor.UpdatedBy = callerId;
        distributor.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} updated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        return MapToDto(distributor);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        await _repo.DeleteAsync(id, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} deleted", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        distributor.IsActive = true;
        distributor.UpdatedBy = callerId;
        distributor.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} activated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        distributor.IsActive = false;
        distributor.UpdatedBy = callerId;
        distributor.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} deactivated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
    }

    private static DistributorDto MapToDto(Distributor d) => new(
        Id: d.Id,
        Name: d.Name,
        Address: d.Address,
        Phone: d.Phone,
        Email: d.Email,
        Alias: d.Alias,
        TradeDiscount: d.TradeDiscount,
        Commission: d.Commission,
        Category: d.Category,
        Remark: d.Remark,
        VatRegNo: d.VatRegNo,
        Latitude: d.Latitude,
        Longitude: d.Longitude,
        TerritoryId: d.TerritoryId,
        TerritoryName: d.Territory?.Name,
        AreaId: d.AreaId,
        RegionId: d.RegionId,
        FleetId: d.FleetId,
        FleetName: d.Fleet?.Name,
        IsActive: d.IsActive,
        CreatedAt: d.CreatedAt,
        UpdatedAt: d.UpdatedAt
    );
}
