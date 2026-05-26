using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Users.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetUserByPhoneAsync(string phone, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, int excludeUserId, CancellationToken ct = default);
    Task<bool> ExistsByUsernameAsync(string username, int excludeUserId, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, int excludeUserId, CancellationToken ct = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetAllUsersAsync(int skip, int take, string? search = null, string? role = null, CancellationToken ct = default);
    Task<Dictionary<int, string?>> GetNamesByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task CreateUserAsync(User user, CancellationToken ct = default);
    Task UpdateUserAsync(User user, CancellationToken ct = default);
    Task DeleteUserAsync(int userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<string?> GetFcmTokenByUserIdAsync(int userId, CancellationToken ct = default);
    Task<List<(int UserId, string Token)>> GetFcmTokensByDistributorIdAsync(int distributorId, CancellationToken ct = default);
    Task<List<(int UserId, string Token)>> GetFcmTokensByDistributorSalesRepsAsync(int distributorId, CancellationToken ct = default);
    Task UpdateFcmTokenAsync(int userId, string? token, CancellationToken ct = default);
    Task ClearFcmTokenAsync(int userId, CancellationToken ct = default);
}
