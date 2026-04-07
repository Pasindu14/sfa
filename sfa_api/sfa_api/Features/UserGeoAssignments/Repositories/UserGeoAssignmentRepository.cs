using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.UserGeoAssignments.Repositories;

public class UserGeoAssignmentRepository(AppDbContext context) : IUserGeoAssignmentRepository
{
    private readonly AppDbContext _context = context;

    public async Task<UserGeoAssignment?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.UserGeoAssignments
            .Include(g => g.User)
            .Include(g => g.Division)
            .Include(g => g.Territory)
            .Include(g => g.Area)
            .Include(g => g.Region)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<UserGeoAssignment?> GetActiveByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.UserGeoAssignments
            .FirstOrDefaultAsync(g => g.UserId == userId && g.IsActive, ct);

    public async Task<(IEnumerable<UserGeoAssignment>, int)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        string? role = null,
        int? regionId = null,
        int? areaId = null,
        int? territoryId = null,
        int? divisionId = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _context.UserGeoAssignments
            .Include(g => g.User)
            .Include(g => g.Division)
            .Include(g => g.Territory)
            .Include(g => g.Area)
            .Include(g => g.Region)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(g => g.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(g => EF.Functions.ILike(g.User!.Name, pattern))
                : query.Where(g => EF.Functions.Like(g.User!.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var parsedRole))
            query = query.Where(g => g.User!.Role == parsedRole);

        if (regionId.HasValue)
            query = query.Where(g => g.RegionId == regionId.Value);

        if (areaId.HasValue)
            query = query.Where(g => g.AreaId == areaId.Value);

        if (territoryId.HasValue)
            query = query.Where(g => g.TerritoryId == territoryId.Value);

        if (divisionId.HasValue)
            query = query.Where(g => g.DivisionId == divisionId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .OrderBy(g => g.User!.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<UserReportingLine>> GetActiveReportingLinesByUserIdsAsync(
        IEnumerable<int> userIds, CancellationToken ct = default)
    {
        var idList = userIds.ToList();
        return await _context.UserReportingLines
            .Include(rl => rl.ReportsToUser)
            .Where(rl => idList.Contains(rl.UserId) && rl.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<(int Total, int Active, int ActiveTerritories, int ThisMonth)> GetStatsAsync(
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var total = await _context.UserGeoAssignments.CountAsync(ct);
        var active = await _context.UserGeoAssignments.CountAsync(g => g.IsActive, ct);
        var activeTerritories = await _context.UserGeoAssignments
            .Where(g => g.IsActive && g.TerritoryId != null)
            .Select(g => g.TerritoryId)
            .Distinct()
            .CountAsync(ct);
        var thisMonth = await _context.UserGeoAssignments
            .CountAsync(g => g.CreatedAt.Year == now.Year && g.CreatedAt.Month == now.Month, ct);

        return (total, active, activeTerritories, thisMonth);
    }

    public async Task<Division?> GetDivisionWithAncestorsAsync(int divisionId, CancellationToken ct = default)
        => await _context.Divisions
            .Include(d => d.Territory)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .IgnoreQueryFilters()                        // load even if division is inactive
            .FirstOrDefaultAsync(d => d.Id == divisionId, ct);

    public async Task<IEnumerable<RouteEntity>> GetActiveRoutesByDivisionIdAsync(int divisionId, CancellationToken ct = default)
        => await _context.Routes
            .Where(r => r.IsActive && r.DivisionId == divisionId)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<bool> UserExistsAsync(int userId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted, ct);

    public async Task<bool> IsAdminOrDistributorAsync(int userId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(
            u => u.Id == userId && (u.Role == UserRole.Admin || u.Role == UserRole.Distributor), ct);

    public async Task<UserRole?> GetUserRoleAsync(int userId, CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> DivisionExistsAsync(int divisionId, CancellationToken ct = default)
        => await _context.Divisions.IgnoreQueryFilters().AnyAsync(d => d.Id == divisionId, ct);

    public async Task CreateAsync(UserGeoAssignment entity, CancellationToken ct = default)
        => await _context.UserGeoAssignments.AddAsync(entity, ct);

    public Task UpdateAsync(UserGeoAssignment entity, CancellationToken ct = default)
    {
        _context.UserGeoAssignments.Update(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
