using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.UserReportingLines.Repositories;

public class UserReportingLineRepository(AppDbContext context) : IUserReportingLineRepository
{
    private readonly AppDbContext _context = context;

    public async Task<UserReportingLine?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.UserReportingLines
            .Include(rl => rl.User)
            .Include(rl => rl.ReportsToUser)
            .FirstOrDefaultAsync(rl => rl.Id == id, ct);

    public async Task<UserReportingLine?> GetActiveByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.UserReportingLines
            .FirstOrDefaultAsync(rl => rl.UserId == userId && rl.IsActive, ct);

    public async Task<(IEnumerable<UserReportingLine> Items, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        string? role = null,
        int? reportsToUserId = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _context.UserReportingLines.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(rl => EF.Functions.ILike(rl.User!.Name, pattern))
                : query.Where(rl => EF.Functions.Like(rl.User!.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var parsedRole))
            query = query.Where(rl => rl.User!.Role == parsedRole);

        if (reportsToUserId.HasValue)
            query = query.Where(rl => rl.ReportsToUserId == reportsToUserId.Value);

        if (isActive.HasValue)
            query = query.Where(rl => rl.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(rl => rl.User)
            .Include(rl => rl.ReportsToUser)
            .AsNoTracking()
            .OrderBy(rl => rl.User!.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<UserReportingLine>> GetDirectReportsAsync(int managerId, CancellationToken ct = default)
        => await _context.UserReportingLines
            .Include(rl => rl.User)
            .Include(rl => rl.ReportsToUser)
            .Where(rl => rl.ReportsToUserId == managerId && rl.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<bool> UserExistsAsync(int userId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted, ct);

    public async Task<bool> IsAdminOrDistributorAsync(int userId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(
            u => u.Id == userId && (u.Role == UserRole.Admin || u.Role == UserRole.Distributor),
            ct);

    public async Task<Dictionary<int, int>> GetActiveLinesForUsersAsync(IEnumerable<int> userIds, CancellationToken ct = default)
        => await _context.UserReportingLines
            .Where(rl => userIds.Contains(rl.UserId) && rl.IsActive && !rl.IsDeleted)
            .AsNoTracking()
            .ToDictionaryAsync(rl => rl.UserId, rl => rl.ReportsToUserId, ct);

    public async Task CreateAsync(UserReportingLine line, CancellationToken ct = default)
        => await _context.UserReportingLines.AddAsync(line, ct);

    public Task UpdateAsync(UserReportingLine line, CancellationToken ct = default)
    {
        _context.UserReportingLines.Update(line);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
