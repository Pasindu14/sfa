using sfa_api.Common.Errors;
using sfa_api.Features.UserGeoAssignments.DTOs;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.UserReportingLines.Repositories;

namespace sfa_api.Features.UserGeoAssignments.Services;

public class UserGeoAssignmentService(
    IUserGeoAssignmentRepository repo,
    IUserReportingLineRepository rlRepo,
    ILogger<UserGeoAssignmentService> logger) : IUserGeoAssignmentService
{
    private readonly IUserGeoAssignmentRepository _repo = repo;
    private readonly IUserReportingLineRepository _rlRepo = rlRepo;
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
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, role, regionId, isActive, ct);

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
        // Validate users exist
        if (!await _repo.UserExistsAsync(request.UserId, ct))
            throw new NotFoundException("User", request.UserId);

        if (!await _repo.UserExistsAsync(request.ReportsToUserId, ct))
            throw new NotFoundException("User", request.ReportsToUserId);

        // Admin and Distributor roles cannot be assigned
        if (await _repo.IsAdminOrDistributorAsync(request.UserId, ct))
            throw new BusinessRuleException(
                "USER_ROLE_NOT_ASSIGNABLE",
                "Admin and Distributor users cannot be assigned a reporting line.");

        // Resolve division ancestors (if a division was specified)
        int? territoryId = null, areaId = null, regionId = null;
        if (request.DivisionId.HasValue)
        {
            var division = await _repo.GetDivisionWithAncestorsAsync(request.DivisionId.Value, ct)
                ?? throw new NotFoundException("Division", request.DivisionId.Value);
            territoryId = division.TerritoryId;
            areaId = division.AreaId;
            regionId = division.RegionId;
        }

        var now = DateTime.UtcNow;

        // 1. Deactivate existing reporting line for this user
        var existingRl = await _rlRepo.GetActiveByUserIdAsync(request.UserId, ct);
        if (existingRl is not null)
        {
            existingRl.IsActive = false;
            existingRl.UpdatedBy = callerId;
            existingRl.UpdatedAt = now;
            await _rlRepo.UpdateAsync(existingRl, ct);
        }

        // 2. Create new reporting line
        var newRl = new UserReportingLine
        {
            UserId = request.UserId,
            ReportsToUserId = request.ReportsToUserId,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _rlRepo.CreateAsync(newRl, ct);

        // 3. Deactivate existing geo assignment for this user
        var existingGeo = await _repo.GetActiveByUserIdAsync(request.UserId, ct);
        if (existingGeo is not null)
        {
            existingGeo.IsActive = false;
            existingGeo.UpdatedBy = callerId;
            existingGeo.UpdatedAt = now;
            await _repo.UpdateAsync(existingGeo, ct);
        }

        // 4. Create new geo assignment with denormalized ancestor IDs
        var newGeo = new UserGeoAssignment
        {
            UserId = request.UserId,
            DivisionId = request.DivisionId,
            TerritoryId = territoryId,
            AreaId = areaId,
            RegionId = regionId,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repo.CreateAsync(newGeo, ct);

        // Single SaveChanges — atomically commits all four operations
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "UserAssignment created: user {UserId} → manager {ManagerId}, division {DivisionId}",
            request.UserId, request.ReportsToUserId, request.DivisionId);

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

        if (!await _repo.UserExistsAsync(request.ReportsToUserId, ct))
            throw new NotFoundException("User", request.ReportsToUserId);

        if (geo.UserId == request.ReportsToUserId)
            throw new BusinessRuleException(
                "SELF_REPORTING_NOT_ALLOWED",
                "A user cannot report to themselves.");

        // Resolve division ancestors
        int? territoryId = null, areaId = null, regionId = null;
        if (request.DivisionId.HasValue)
        {
            var division = await _repo.GetDivisionWithAncestorsAsync(request.DivisionId.Value, ct)
                ?? throw new NotFoundException("Division", request.DivisionId.Value);
            territoryId = division.TerritoryId;
            areaId = division.AreaId;
            regionId = division.RegionId;
        }

        var now = DateTime.UtcNow;

        // Deactivate existing reporting line, create new one with updated manager
        var existingRl = await _rlRepo.GetActiveByUserIdAsync(geo.UserId, ct);
        if (existingRl is not null)
        {
            existingRl.IsActive = false;
            existingRl.UpdatedBy = callerId;
            existingRl.UpdatedAt = now;
            await _rlRepo.UpdateAsync(existingRl, ct);
        }

        var newRl = new UserReportingLine
        {
            UserId = geo.UserId,
            ReportsToUserId = request.ReportsToUserId,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _rlRepo.CreateAsync(newRl, ct);

        // Update geo assignment in-place
        geo.DivisionId = request.DivisionId;
        geo.TerritoryId = territoryId;
        geo.AreaId = areaId;
        geo.RegionId = regionId;
        geo.EffectiveFrom = request.EffectiveFrom;
        geo.UpdatedBy = callerId;
        geo.UpdatedAt = now;
        await _repo.UpdateAsync(geo, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("UserAssignment {Id} updated", id);

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

        // Deactivate geo assignment
        geo.IsActive = false;
        geo.UpdatedBy = callerId;
        geo.UpdatedAt = now;
        await _repo.UpdateAsync(geo, ct);

        // Deactivate the active reporting line for this user
        var rl = await _rlRepo.GetActiveByUserIdAsync(geo.UserId, ct);
        if (rl is not null)
        {
            rl.IsActive = false;
            rl.UpdatedBy = callerId;
            rl.UpdatedAt = now;
            await _rlRepo.UpdateAsync(rl, ct);
        }

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "UserAssignment {Id} deactivated (userId={UserId})", id, geo.UserId);
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
