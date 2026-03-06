using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Auth.Repositories;

public class AuthRepository(AppDbContext db) : IAuthRepository
{
    private readonly AppDbContext _db = db;

    #pragma warning disable CA1862
    public async Task<User?> GetUserByEmailAsync(
        string email, CancellationToken ct = default)
        => await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() && x.IsActive, ct);

    public async Task<User?> GetUserByUsernameAsync(
        string username, CancellationToken ct = default)
        => await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower() && x.IsActive, ct);

    public async Task<User?> GetUserByIdAsync(
        int userId, CancellationToken ct = default)
        => await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, ct);

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(
        string tokenHash, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public async Task AddRefreshTokenAsync(
        RefreshToken token, CancellationToken ct = default)
        => await _db.RefreshTokens.AddAsync(token, ct);

    public async Task RevokeTokenFamilyAsync(
        Guid familyId, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Where(x => x.FamilyId == familyId && !x.IsRevoked)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.IsRevoked, true), ct);

    public async Task RevokeAllUserTokensAsync(
        int userId, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.IsRevoked, true), ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
