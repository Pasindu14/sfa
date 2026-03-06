using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Users.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    private readonly AppDbContext _context = context;

    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
        => await _context.Users.FindAsync([userId], ct);

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

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetAllUsersAsync(int skip, int take, CancellationToken ct = default)
    {
        var totalCount = await _context.Users.CountAsync(ct);
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (users, totalCount);
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
            _context.Users.Update(user);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
