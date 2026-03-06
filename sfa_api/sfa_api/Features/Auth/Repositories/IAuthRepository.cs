using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Auth.Repositories;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeTokenFamilyAsync(Guid familyId, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(int userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
