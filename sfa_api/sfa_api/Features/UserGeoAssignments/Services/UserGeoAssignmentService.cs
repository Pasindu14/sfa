using sfa_api.Common.Errors;
using sfa_api.Features.UserGeoAssignments.DTOs;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.UserGeoAssignments.Services;

public class UserGeoAssignmentService(
    IUserGeoAssignmentRepository repo,
    ILogger<UserGeoAssignmentService> logger) : IUserGeoAssignmentService
{
    private readonly IUserGeoAssignmentRepository _repo = repo;
    private readonly ILogger<UserGeoAssignmentService> _logger = logger;

    public async Task<UserAssignmentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var geo = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserAssignment", id);

        var rls = await _repo.GetActiveReportingLinesByUserIdsAsync([geo.UserId], ct);
        return MapToDto(geo, rls.FirstOrDefault());
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

        // Validate division if provided
        if (request.DivisionId.HasValue &&
            !await _repo.DivisionExistsAsync(request.DivisionId.Value, ct))
            throw new NotFoundException("Division", request.DivisionId.Value);

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

        // Create geo assignment — all 4 geo IDs supplied directly by the caller
        var newGeo = new UserGeoAssignment
        {
            UserId = request.UserId,
            RegionId = request.RegionId,
            AreaId = request.AreaId,
            TerritoryId = request.TerritoryId,
            DivisionId = request.DivisionId,
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
            request.UserId, request.RegionId, request.AreaId, request.TerritoryId, request.DivisionId);

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

        var now = DateTime.UtcNow;

        geo.RegionId = request.RegionId;
        geo.AreaId = request.AreaId;
        geo.TerritoryId = request.TerritoryId;
        geo.DivisionId = request.DivisionId;
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
