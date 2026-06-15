using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.UserGeoAssignments.DTOs;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.UserGeoAssignments.Services;

public class UserGeoAssignmentService(
    IUserGeoAssignmentRepository repo,
    IDistributorRepository distributorRepo,
    ILogger<UserGeoAssignmentService> logger) : IUserGeoAssignmentService
{
    private readonly IUserGeoAssignmentRepository _repo = repo;
    private readonly IDistributorRepository _distributorRepo = distributorRepo;
    private readonly ILogger<UserGeoAssignmentService> _logger = logger;

    public async Task<UserAssignmentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var geo = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserAssignment", id);

        var rls = await _repo.GetActiveReportingLinesByUserIdsAsync([geo.UserId], ct);
        return MapToDto(geo, rls.FirstOrDefault());
    }

    public async Task<RepAssignmentDto> GetMyAssignmentAsync(int userId, CancellationToken ct = default)
    {
        var geo = await _repo.GetActiveWithDetailsByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("UserAssignment", userId);

        string? distributorName = null;
        int? distributorId = null;
        int? fleetId = null;
        string? fleetName = null;

        if (geo.TerritoryId.HasValue)
        {
            var distributor = await _distributorRepo.GetByTerritoryIdAsync(geo.TerritoryId.Value, ct);
            distributorId = distributor?.Id;
            distributorName = distributor?.Name;
            fleetId = distributor?.FleetId;
            fleetName = distributor?.Fleet?.Name;
        }

        return new RepAssignmentDto(
            DivisionId: geo.DivisionId,
            DivisionName: geo.Division?.Name,
            TerritoryId: geo.TerritoryId,
            TerritoryName: geo.Territory?.Name,
            DistributorId: distributorId,
            DistributorName: distributorName,
            FleetId: fleetId,
            FleetName: fleetName
        );
    }

    public async Task<UserAssignmentListDto> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        int? regionId = null,
        int? areaId = null,
        int? territoryId = null,
        int? divisionId = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, role, regionId, areaId, territoryId, divisionId, isActive, ct);

        var itemList = items.ToList();
        var userIds = itemList.Select(g => g.UserId).Distinct();
        var rlByUserId = (await _repo.GetActiveReportingLinesByUserIdsAsync(userIds, ct))
            .ToDictionary(rl => rl.UserId);

        return new UserAssignmentListDto(
            UserAssignments: itemList.Select(g => MapToDto(g, rlByUserId.GetValueOrDefault(g.UserId))),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<UserAssignmentStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var (total, active, activeTerritories, thisMonth) = await _repo.GetStatsAsync(ct);
        return new UserAssignmentStatsDto(total, active, activeTerritories, thisMonth);
    }

    public async Task<UserAssignmentDto> CreateAsync(
        CreateUserAssignmentRequest request,
        int? callerId,
        CancellationToken ct = default)
    {
        // Validate user exists
        if (!await _repo.UserExistsAsync(request.UserId, ct))
            throw new NotFoundException("User", request.UserId);

        // Admin and Distributor roles cannot be geo-assigned
        if (await _repo.IsAdminOrDistributorAsync(request.UserId, ct))
            throw new BusinessRuleException(
                "USER_ROLE_NOT_ASSIGNABLE",
                "Admin and Distributor users cannot be given a geo assignment.");

        // SalesRep must be assigned to a Division
        var userRole = await _repo.GetUserRoleAsync(request.UserId, ct);
        if (userRole == UserRole.SalesRep && !request.DivisionId.HasValue)
            throw new BusinessRuleException(
                "DIVISION_REQUIRED_FOR_SALES_REP",
                "SalesRep users must be assigned to a Division.");

        // Derive ancestor IDs from the Division — never trust client-supplied ancestors.
        var (regionId, areaId, territoryId, divisionId) = await ResolveGeoIdsAsync(
            request.RegionId, request.AreaId, request.TerritoryId, request.DivisionId, ct);

        var now = DateTime.UtcNow;

        // Deactivate existing geo assignment for this user
        var existingGeo = await _repo.GetActiveByUserIdAsync(request.UserId, ct);
        if (existingGeo is not null)
        {
            existingGeo.IsActive = false;
            existingGeo.UpdatedBy = callerId;
            existingGeo.UpdatedAt = now;
            await _repo.UpdateAsync(existingGeo, ct);
        }

        var newGeo = new UserGeoAssignment
        {
            UserId = request.UserId,
            RegionId = regionId,
            AreaId = areaId,
            TerritoryId = territoryId,
            DivisionId = divisionId,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repo.CreateAsync(newGeo, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "GeoAssignment created: user {UserId}, region {RegionId}, area {AreaId}, territory {TerritoryId}, division {DivisionId}",
            request.UserId, regionId, areaId, territoryId, divisionId);

        var created = await _repo.GetByIdAsync(newGeo.Id, ct)
            ?? throw new NotFoundException("UserAssignment", newGeo.Id);
        var rls = await _repo.GetActiveReportingLinesByUserIdsAsync([created.UserId], ct);
        return MapToDto(created, rls.FirstOrDefault());
    }

    public async Task<UserAssignmentDto> UpdateAsync(
        int id,
        UpdateUserAssignmentRequest request,
        int? callerId,
        CancellationToken ct = default)
    {
        var geo = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserAssignment", id);

        // SalesRep must be assigned to a Division
        var userRole = await _repo.GetUserRoleAsync(geo.UserId, ct);
        if (userRole == UserRole.SalesRep && !request.DivisionId.HasValue)
            throw new BusinessRuleException(
                "DIVISION_REQUIRED_FOR_SALES_REP",
                "SalesRep users must be assigned to a Division.");

        // Derive ancestor IDs from the Division — never trust client-supplied ancestors.
        var (regionId, areaId, territoryId, divisionId) = await ResolveGeoIdsAsync(
            request.RegionId, request.AreaId, request.TerritoryId, request.DivisionId, ct);

        var now = DateTime.UtcNow;

        geo.RegionId = regionId;
        geo.AreaId = areaId;
        geo.TerritoryId = territoryId;
        geo.DivisionId = divisionId;
        geo.EffectiveFrom = request.EffectiveFrom;
        geo.UpdatedBy = callerId;
        geo.UpdatedAt = now;
        await _repo.UpdateAsync(geo, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("GeoAssignment {Id} updated", id);

        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserAssignment", id);
        var rls = await _repo.GetActiveReportingLinesByUserIdsAsync([updated.UserId], ct);
        return MapToDto(updated, rls.FirstOrDefault());
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var geo = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserAssignment", id);

        var now = DateTime.UtcNow;

        geo.IsActive = false;
        geo.UpdatedBy = callerId;
        geo.UpdatedAt = now;
        await _repo.UpdateAsync(geo, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "GeoAssignment {Id} deactivated (userId={UserId})", id, geo.UserId);
    }

    public async Task<UserAssignmentDto> ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var geo = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserAssignment", id);

        var now = DateTime.UtcNow;

        geo.IsActive = true;
        geo.UpdatedBy = callerId;
        geo.UpdatedAt = now;
        await _repo.UpdateAsync(geo, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("GeoAssignment {Id} activated (userId={UserId})", id, geo.UserId);

        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserAssignment", id);
        var rls = await _repo.GetActiveReportingLinesByUserIdsAsync([updated.UserId], ct);
        return MapToDto(updated, rls.FirstOrDefault());
    }

    /// <summary>
    /// Resolves the four geo IDs that get persisted. When a Division is supplied, all ancestor
    /// IDs (Territory/Area/Region) are taken from the Division entity itself — the client's
    /// ancestor IDs are ignored so a Division can never be stored under a mismatched ancestry.
    /// Higher-level assignments without a Division keep the supplied IDs.
    /// </summary>
    private async Task<(int? RegionId, int? AreaId, int? TerritoryId, int? DivisionId)> ResolveGeoIdsAsync(
        int? regionId, int? areaId, int? territoryId, int? divisionId, CancellationToken ct)
    {
        if (!divisionId.HasValue)
            return (regionId, areaId, territoryId, null);

        var division = await _repo.GetDivisionWithAncestorsAsync(divisionId.Value, ct)
            ?? throw new NotFoundException("Division", divisionId.Value);

        return (division.RegionId, division.AreaId, division.TerritoryId, division.Id);
    }

    private static UserAssignmentDto MapToDto(
        UserGeoAssignment geo,
        UserReportingLine? rl) => new(
        Id: geo.Id,
        UserId: geo.UserId,
        UserName: geo.User?.Name ?? string.Empty,
        UserRole: geo.User?.Role.ToString() ?? string.Empty,
        ReportsToUserId: rl?.ReportsToUserId,
        ReportsToUserName: rl?.ReportsToUser?.Name,
        DivisionId: geo.DivisionId,
        DivisionName: geo.Division?.Name,
        TerritoryId: geo.TerritoryId,
        TerritoryName: geo.Territory?.Name,
        AreaId: geo.AreaId,
        AreaName: geo.Area?.Name,
        RegionId: geo.RegionId,
        RegionName: geo.Region?.Name,
        EffectiveFrom: geo.EffectiveFrom,
        IsActive: geo.IsActive,
        CreatedAt: geo.CreatedAt,
        UpdatedAt: geo.UpdatedAt
    );
}
