using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Users.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetUserByPhoneAsync(string phone, CancellationToken ct = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetAllUsersAsync(int skip, int take, CancellationToken ct = default);
    Task CreateUserAsync(User user, CancellationToken ct = default);
    Task UpdateUserAsync(User user, CancellationToken ct = default);
    Task DeleteUserAsync(int userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
