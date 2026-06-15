using Microsoft.EntityFrameworkCore;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Users.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    private readonly AppDbContext _context = context;

    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.Distributor)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<User?> GetUserByPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Username == username, ct);

    public async Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Phone == phone, ct);

    public async Task<bool> ExistsByEmailAsync(string email, int excludeUserId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Email == email && u.Id != excludeUserId, ct);

    public async Task<bool> ExistsByUsernameAsync(string username, int excludeUserId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Username == username && u.Id != excludeUserId, ct);

    public async Task<bool> ExistsByPhoneAsync(string phone, int excludeUserId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Phone == phone && u.Id != excludeUserId, ct);

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetAllUsersAsync(int skip, int take, string? search = null, string? role = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Users.Include(u => u.Distributor).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Username, pattern) || EF.Functions.ILike(u.Email, pattern) || EF.Functions.ILike(u.Phone, pattern))
                : query.Where(u => EF.Functions.Like(u.Name, pattern) || EF.Functions.Like(u.Username, pattern) || EF.Functions.Like(u.Email, pattern) || EF.Functions.Like(u.Phone, pattern));
        }
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var parsedRole))
            query = query.Where(u => u.Role == parsedRole);

        var totalCount = await query.CountAsync(ct);
        var users = await query
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (users, totalCount);
    }

    public async Task<Dictionary<int, string?>> GetNamesByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await _context.Users
            .Where(u => idList.Contains(u.Id))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => (string?)u.Name, ct);
    }

    public async Task CreateUserAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);

    public Task UpdateUserAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task DeleteUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync([userId], ct);
        if (user != null)
        {
            user.IsDeleted = true;
            user.IsActive = false; // a deleted user must also be inactive (cannot log in)
            _context.Users.Update(user);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task<string?> GetFcmTokenByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => u.FcmToken)
            .FirstOrDefaultAsync(ct);

    public async Task<List<(int UserId, string Token)>> GetFcmTokensByDistributorIdAsync(int distributorId, CancellationToken ct = default)
    {
        var rows = await _context.Users
            .Where(u => u.DistributorId == distributorId && !u.IsDeleted && u.FcmToken != null)
            .Select(u => new { u.Id, Token = u.FcmToken! })
            .AsNoTracking()
            .ToListAsync(ct);
        return rows.Select(r => (r.Id, r.Token)).ToList();
    }

    public async Task<List<(int UserId, string Token)>> GetFcmTokensByDistributorSalesRepsAsync(int distributorId, CancellationToken ct = default)
    {
        // SalesReps are not linked to distributors via User.DistributorId (that field is for Distributor-role users only).
        // The relationship is: SalesRep → UserGeoAssignment.TerritoryId ← Distributor.TerritoryId
        var rows = await (
            from u in _context.Users
            join geo in _context.UserGeoAssignments on u.Id equals geo.UserId
            join d in _context.Distributors on geo.TerritoryId equals d.TerritoryId
            where d.Id == distributorId
                && u.Role == UserRole.SalesRep
                && !u.IsDeleted
                && u.FcmToken != null
                && geo.IsActive
                && !geo.IsDeleted
            select new { u.Id, Token = u.FcmToken! }
        ).Distinct().AsNoTracking().ToListAsync(ct);
        return rows.Select(r => (r.Id, r.Token)).ToList();
    }

    public async Task UpdateFcmTokenAsync(int userId, string? token, CancellationToken ct = default)
        => await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.FcmToken, token)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);

    public Task ClearFcmTokenAsync(int userId, CancellationToken ct = default)
        => UpdateFcmTokenAsync(userId, null, ct);
}
