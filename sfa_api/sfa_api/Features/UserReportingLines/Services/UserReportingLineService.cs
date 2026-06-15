using sfa_api.Common.Errors;
using sfa_api.Features.UserReportingLines.DTOs;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Features.UserReportingLines.Requests;

namespace sfa_api.Features.UserReportingLines.Services;

public class UserReportingLineService(
    IUserReportingLineRepository repo,
    ILogger<UserReportingLineService> logger) : IUserReportingLineService
{
    private readonly IUserReportingLineRepository _repo = repo;
    private readonly ILogger<UserReportingLineService> _logger = logger;

    public async Task<UserReportingLineDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var line = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserReportingLine", id);
        return MapToDto(line);
    }

    public async Task<UserReportingLineListDto> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        int? reportsToUserId = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, role, reportsToUserId, isActive, ct);
        return new UserReportingLineListDto(
            UserReportingLines: items.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<IEnumerable<UserReportingLineDto>> GetSubordinatesAsync(
        int userId,
        bool directOnly,
        CancellationToken ct = default)
    {
        if (!await _repo.UserExistsAsync(userId, ct))
            throw new NotFoundException("User", userId);

        // Anchor — get direct reports of the target manager
        var directReports = (await _repo.GetDirectReportsAsync(userId, ct)).ToList();

        if (directOnly)
            return directReports.Select(MapToDto);

        // BFS — traverse the full subtree (max ~5 levels for a sales org)
        var result = new List<UserReportingLine>(directReports);
        var visited = new HashSet<int>(directReports.Select(r => r.UserId)) { userId };
        var queue = new Queue<int>(directReports.Select(r => r.UserId));

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            var children = await _repo.GetDirectReportsAsync(parentId, ct);

            foreach (var child in children)
            {
                if (visited.Add(child.UserId))
                {
                    result.Add(child);
                    queue.Enqueue(child.UserId);
                }
            }
        }

        return result.Select(MapToDto);
    }

    public async Task<UserReportingLineDto> CreateAsync(
        CreateUserReportingLineRequest request,
        int? callerId,
        CancellationToken ct = default)
    {
        if (!await _repo.UserExistsAsync(request.UserId, ct))
            throw new NotFoundException("User", request.UserId);

        if (!await _repo.UserExistsAsync(request.ReportsToUserId, ct))
            throw new NotFoundException("User", request.ReportsToUserId);

        // Admin and Distributor roles are not assignable as subordinates
        if (await _repo.IsAdminOrDistributorAsync(request.UserId, ct))
            throw new BusinessRuleException(
                "USER_ROLE_NOT_ASSIGNABLE",
                "Admin and Distributor users cannot be assigned a reporting line.");

        // Reject reporting cycles (e.g. A→B then B→A, at any depth)
        if (await WouldCreateCycleAsync(request.UserId, request.ReportsToUserId, ct))
            throw new BusinessRuleException(
                "REPORTING_CYCLE",
                "This assignment would create a reporting cycle: the chosen manager already reports to this user (directly or indirectly).");

        // Deactivate the existing active line for this user if one exists
        var existingLine = await _repo.GetActiveByUserIdAsync(request.UserId, ct);
        if (existingLine is not null)
        {
            existingLine.IsActive = false;
            existingLine.UpdatedBy = callerId;
            existingLine.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(existingLine, ct);
            _logger.LogInformation(
                "Deactivated previous reporting line {LineId} for user {UserId}",
                existingLine.Id, request.UserId);
        }

        var line = new UserReportingLine
        {
            UserId = request.UserId,
            ReportsToUserId = request.ReportsToUserId,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(line, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "UserReportingLine {Id} created: user {UserId} now reports to {ManagerId}",
            line.Id, request.UserId, request.ReportsToUserId);

        var created = await _repo.GetByIdAsync(line.Id, ct)
            ?? throw new NotFoundException("UserReportingLine", line.Id);
        return MapToDto(created);
    }

    public async Task<UserReportingLineDto> UpdateAsync(
        int id,
        UpdateUserReportingLineRequest request,
        int? callerId,
        CancellationToken ct = default)
    {
        var line = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserReportingLine", id);

        if (!await _repo.UserExistsAsync(request.ReportsToUserId, ct))
            throw new NotFoundException("User", request.ReportsToUserId);

        // Guard against self-reference (UpdateUserReportingLineRequest doesn't carry UserId
        // but we validate here too to be safe)
        if (line.UserId == request.ReportsToUserId)
            throw new BusinessRuleException(
                "SELF_REPORTING_NOT_ALLOWED",
                "A user cannot report to themselves.");

        // Reject reporting cycles at any depth
        if (await WouldCreateCycleAsync(line.UserId, request.ReportsToUserId, ct))
            throw new BusinessRuleException(
                "REPORTING_CYCLE",
                "This assignment would create a reporting cycle: the chosen manager already reports to this user (directly or indirectly).");

        line.ReportsToUserId = request.ReportsToUserId;
        line.EffectiveFrom = request.EffectiveFrom;
        line.UpdatedBy = callerId;
        line.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(line, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("UserReportingLine {Id} updated", id);

        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserReportingLine", id);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var line = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserReportingLine", id);

        line.IsActive = false;
        line.UpdatedBy = callerId;
        line.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(line, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("UserReportingLine {Id} deactivated", id);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var line = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("UserReportingLine", id);

        line.IsActive = true;
        line.UpdatedBy = callerId;
        line.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(line, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("UserReportingLine {Id} activated", id);
    }

    /// <summary>
    /// Returns true if making <paramref name="userId"/> report to <paramref name="managerId"/>
    /// would create a cycle — i.e. the manager already reports (directly or transitively)
    /// to the user. Walks UP the active reporting chain from the proposed manager.
    /// </summary>
    private async Task<bool> WouldCreateCycleAsync(int userId, int managerId, CancellationToken ct)
    {
        var currentId = managerId;
        var visited = new HashSet<int>();

        while (true)
        {
            if (currentId == userId)
                return true; // the chain loops back to the user → cycle

            if (!visited.Add(currentId))
                return false; // a pre-existing loop not involving the user; stop safely

            var managerLine = await _repo.GetActiveByUserIdAsync(currentId, ct);
            if (managerLine is null)
                return false; // reached the top of the hierarchy, no cycle

            currentId = managerLine.ReportsToUserId;
        }
    }

    private static UserReportingLineDto MapToDto(UserReportingLine rl) => new(
        Id: rl.Id,
        UserId: rl.UserId,
        UserName: rl.User?.Name ?? string.Empty,
        UserRole: rl.User?.Role.ToString() ?? string.Empty,
        ReportsToUserId: rl.ReportsToUserId,
        ReportsToUserName: rl.ReportsToUser?.Name ?? string.Empty,
        ReportsToUserRole: rl.ReportsToUser?.Role.ToString() ?? string.Empty,
        EffectiveFrom: rl.EffectiveFrom,
        IsActive: rl.IsActive,
        CreatedAt: rl.CreatedAt,
        UpdatedAt: rl.UpdatedAt
    );
}
