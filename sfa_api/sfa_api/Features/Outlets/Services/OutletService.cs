using sfa_api.Common.Errors;
using sfa_api.Features.Outlets.DTOs;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Outlets.Repositories;
using sfa_api.Features.Outlets.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Outlets.Services;

public class OutletService(
    IOutletRepository repo,
    ICacheService cache,
    ILogger<OutletService> logger) : IOutletService
{
    private readonly IOutletRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<OutletService> _logger = logger;

    private static readonly TimeSpan RouteCacheTtl = TimeSpan.FromMinutes(30);
    private const string RouteCachePrefix = "outlets:route:";

    public async Task<OutletDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var outlet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Outlet", id);
        return MapToDto(outlet);
    }

    public async Task<OutletListDto> GetAllAsync(int page, int pageSize, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (outlets, totalCount) = await _repo.GetAllAsync(skip, pageSize, isActive, search, ct);
        return new OutletListDto(
            Outlets: outlets.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<OutletListDto> GetAllByTerritoryAsync(
        int territoryId, int page, int pageSize,
        bool? isActive = null, string? search = null,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (outlets, totalCount) = await _repo.GetAllByTerritoryAsync(territoryId, skip, pageSize, isActive, search, ct);
        return new OutletListDto(
            Outlets: outlets.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<IEnumerable<OutletDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var outlets = await _repo.GetAllActiveAsync(ct);
        return outlets.Select(MapToDto);
    }

    public async Task<IEnumerable<OutletDto>> GetByRouteIdAsync(int routeId, CancellationToken ct = default)
    {
        var cacheKey = $"{RouteCachePrefix}{routeId}";
        var cached = await _cache.GetAsync<IEnumerable<OutletDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var outlets = await _repo.GetByRouteIdAsync(routeId, ct);
        var result = outlets.Select(MapToDto).ToList();
        await _cache.SetAsync(cacheKey, result, RouteCacheTtl, ct);
        return result;
    }

    public async Task<IEnumerable<OutletMapPointDto>> GetMapPointsAsync(CancellationToken ct = default)
        => await _repo.GetMapPointsAsync(ct);

    public async Task<OutletDto> CreateAsync(CreateOutletRequest request, int? callerId, CancellationToken ct = default)
    {
        var route = await _repo.GetRouteWithAncestorsAsync(request.RouteId, ct)
            ?? throw new NotFoundException("Route", request.RouteId);

        if (!Enum.TryParse<OutletType>(request.OutletType, out var outletType))
            throw new ValidationException(new Dictionary<string, string[]>
                { { "OutletType", new[] { "Invalid OutletType." } } });

        if (!Enum.TryParse<OutletCategory>(request.OutletCategory, out var outletCategory))
            throw new ValidationException(new Dictionary<string, string[]>
                { { "OutletCategory", new[] { "Invalid OutletCategory." } } });

        if (await _repo.ExistsByNicNoAsync(request.NicNo, ct))
            throw new DuplicateResourceException("NicNo");

        var outlet = new Outlet
        {
            Name = request.Name,
            Address = request.Address,
            Tel = request.Tel,
            Email = request.Email,
            ContactPerson = request.ContactPerson,
            NicNo = request.NicNo,
            VatNo = request.VatNo,
            CreditLimit = request.CreditLimit,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            OwnerDOB = request.OwnerDOB,
            Remarks = request.Remarks,
            Image = request.Image,
            OutletType = outletType,
            OutletCategory = outletCategory,
            ProvinceCode = request.ProvinceCode,
            DistrictCode = request.DistrictCode,
            RouteId = route.Id,
            DivisionId = route.DivisionId,
            TerritoryId = route.TerritoryId,
            AreaId = route.AreaId,
            RegionId = route.RegionId,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(outlet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Outlet {OutletId} created", outlet.Id);
        await _cache.RemoveByPrefixAsync(RouteCachePrefix, ct);

        var created = await _repo.GetByIdAsync(outlet.Id, ct)
            ?? throw new NotFoundException("Outlet", outlet.Id);
        return MapToDto(created);
    }

    public async Task<OutletDto> UpdateAsync(int id, UpdateOutletRequest request, int? callerId, CancellationToken ct = default)
    {
        var outlet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Outlet", id);

        var route = await _repo.GetRouteWithAncestorsAsync(request.RouteId, ct)
            ?? throw new NotFoundException("Route", request.RouteId);

        if (!Enum.TryParse<OutletType>(request.OutletType, out var outletType))
            throw new ValidationException(new Dictionary<string, string[]>
                { { "OutletType", new[] { "Invalid OutletType." } } });

        if (!Enum.TryParse<OutletCategory>(request.OutletCategory, out var outletCategory))
            throw new ValidationException(new Dictionary<string, string[]>
                { { "OutletCategory", new[] { "Invalid OutletCategory." } } });

        if (await _repo.ExistsByNicNoAsync(request.NicNo, id, ct))
            throw new DuplicateResourceException("NicNo");

        outlet.Name = request.Name;
        outlet.Address = request.Address;
        outlet.Tel = request.Tel;
        outlet.Email = request.Email;
        outlet.ContactPerson = request.ContactPerson;
        outlet.NicNo = request.NicNo;
        outlet.VatNo = request.VatNo;
        outlet.CreditLimit = request.CreditLimit;
        outlet.Latitude = request.Latitude;
        outlet.Longitude = request.Longitude;
        outlet.OwnerDOB = request.OwnerDOB;
        outlet.Remarks = request.Remarks;
        outlet.Image = request.Image;
        outlet.OutletType = outletType;
        outlet.OutletCategory = outletCategory;
        outlet.ProvinceCode = request.ProvinceCode;
        outlet.DistrictCode = request.DistrictCode;
        outlet.RouteId = route.Id;
        outlet.DivisionId = route.DivisionId;
        outlet.TerritoryId = route.TerritoryId;
        outlet.AreaId = route.AreaId;
        outlet.RegionId = route.RegionId;
        outlet.UpdatedBy = callerId;
        outlet.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(outlet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Outlet {OutletId} updated", id);
        await _cache.RemoveByPrefixAsync(RouteCachePrefix, ct);

        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Outlet", id);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Outlet", id);

        await _repo.DeleteAsync(id, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Outlet {OutletId} deleted", id);
        await _cache.RemoveByPrefixAsync(RouteCachePrefix, ct);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var outlet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Outlet", id);

        outlet.IsActive = true;
        outlet.UpdatedBy = callerId;
        outlet.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(outlet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Outlet {OutletId} activated", id);
        await _cache.RemoveByPrefixAsync(RouteCachePrefix, ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var outlet = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Outlet", id);

        outlet.IsActive = false;
        outlet.UpdatedBy = callerId;
        outlet.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(outlet, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Outlet {OutletId} deactivated", id);
        await _cache.RemoveByPrefixAsync(RouteCachePrefix, ct);
    }

    private static OutletDto MapToDto(Outlet o) => new(
        Id: o.Id,
        Name: o.Name,
        Address: o.Address,
        Tel: o.Tel,
        Email: o.Email,
        ContactPerson: o.ContactPerson,
        NicNo: o.NicNo,
        VatNo: o.VatNo,
        CreditLimit: o.CreditLimit,
        Latitude: o.Latitude,
        Longitude: o.Longitude,
        OwnerDOB: o.OwnerDOB,
        Remarks: o.Remarks,
        Image: o.Image,
        OutletType: o.OutletType.ToString(),
        OutletCategory: o.OutletCategory.ToString(),
        ProvinceCode: o.ProvinceCode,
        DistrictCode: o.DistrictCode,
        RouteId: o.RouteId,
        RouteName: o.Route?.Name ?? string.Empty,
        DivisionId: o.DivisionId,
        DivisionName: o.Route?.Division?.Name ?? string.Empty,
        TerritoryId: o.TerritoryId,
        TerritoryName: o.Route?.Territory?.Name ?? string.Empty,
        AreaId: o.AreaId,
        AreaName: o.Route?.Area?.Name ?? string.Empty,
        RegionId: o.RegionId,
        RegionName: o.Route?.Region?.Name ?? string.Empty,
        IsActive: o.IsActive,
        CreatedAt: o.CreatedAt,
        UpdatedAt: o.UpdatedAt,
        LastBillDate: o.LastBillDate
    );
}
